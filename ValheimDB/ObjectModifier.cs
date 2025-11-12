using System.Collections.Generic;
using System.Linq;
using HarmonyLib;
using UnityEngine;
using ValheimDB.DataTypes;

namespace ValheimDB;

public static class ObjectModifier
{
    private static readonly Dictionary<string, PieceInfoWrapper.PieceInfo> ModifiedPieces = [];
    public static void ApplyPieces()
    {
        ZNetScene zns = ZNetScene.instance;
        if (!zns) return;
        Dictionary<string, PieceInfoWrapper.PieceInfo> data = ValheimDB.PieceInfos.Value;
        foreach (KeyValuePair<string, PieceInfoWrapper.PieceInfo> originalInfo in ModifiedPieces)
        {
            if (zns.GetPrefab(originalInfo.Key) is not GameObject prefab) continue;
            if (prefab.GetComponent<Piece>() is not Piece piece) continue;
            originalInfo.Value.Apply(zns, piece);
        }

        for (int i = 0; i < Piece.s_allPieces.Count; ++i)
        {
            Piece p = Piece.s_allPieces[i];
            string prefabName = global::Utils.GetPrefabName(p.name);
            if (!ModifiedPieces.TryGetValue(prefabName, out PieceInfoWrapper.PieceInfo originalInfo)) continue;
            originalInfo.Apply(zns, p);
        }

        ModifiedPieces.Clear();
        foreach (KeyValuePair<string, PieceInfoWrapper.PieceInfo> newInfo in data)
        {
            if (zns.GetPrefab(newInfo.Key) is not GameObject prefab) continue;
            if (prefab.GetComponent<Piece>() is not Piece piece) continue;
            ModifiedPieces[newInfo.Key] = piece;
            newInfo.Value.Apply(zns, piece);
        }

        for (int i = 0; i < Piece.s_allPieces.Count; ++i)
        {
            Piece p = Piece.s_allPieces[i];
            string prefabName = global::Utils.GetPrefabName(p.name);
            if (!data.TryGetValue(prefabName, out PieceInfoWrapper.PieceInfo newInfo)) continue;
            newInfo.Apply(zns, p);
        }
    }
    private static readonly GameObject _DisabledForClonedItems = new("ValheimDB_DisabledForClonedItems") { hideFlags = HideFlags.HideAndDontSave };
    private static readonly Dictionary<string, ItemInfoWrapper.ItemInfo> ModifiedItems = [];
    private static Dictionary<string, (GameObject go, string from, ItemDrop.ItemData.SharedData sharedData)> ClonedItems = [];
    [HarmonyPatch(typeof(ObjectDB),nameof(ObjectDB.Awake))]
    private static class ObjectDB_Awake_Patch_CopyItems
    {
        private static void Prefix(ObjectDB __instance)
        {
            for(int i = _DisabledForClonedItems.transform.childCount - 1; i >= 0; --i) __instance.m_items.Add(_DisabledForClonedItems.transform.GetChild(i).gameObject);
        }
    }
    public static void ApplyItems()
    {
        void ApplyInventories(List<ItemDrop.ItemData> inventories, Dictionary<string, ItemInfoWrapper.ItemInfo> infoDict)
        {
            for (int i = 0; i < inventories.Count; ++i)
            {
                ItemDrop.ItemData item = inventories[i];
                if (item == null || !item.m_dropPrefab) continue;
                string prefab = item.m_dropPrefab.name;
                if (!infoDict.TryGetValue(prefab, out ItemInfoWrapper.ItemInfo newInfo)) continue;
                newInfo.Apply(item.m_shared);
            }
        }
        
        void ApplyGroundItems(List<ItemDrop> itemsOnGround, Dictionary<string, ItemInfoWrapper.ItemInfo> infoDict)
        {
            for (int i = 0; i < itemsOnGround.Count; ++i)
            {
                ItemDrop itemDrop = itemsOnGround[i];
                string prefab = global::Utils.GetPrefabName(itemDrop.gameObject);
                if (!infoDict.TryGetValue(prefab, out ItemInfoWrapper.ItemInfo newInfo)) continue;
                newInfo.Apply(ZNetScene.instance, itemDrop);
            } 
        }
        
        void CreateClonePrefab(ZNetScene zns, GameObject toClone, ItemInfoWrapper.ItemInfo info, string newName)
        {
            GameObject clonedPrefab = Object.Instantiate(toClone, _DisabledForClonedItems.transform);
            ItemDrop clonedDrop = clonedPrefab.GetComponent<ItemDrop>();
            clonedDrop.m_itemData = clonedDrop.m_itemData.Clone();
            clonedDrop.m_itemData.m_shared = (ItemDrop.ItemData.SharedData)AccessTools.DeclaredMethod(typeof(object), "MemberwiseClone").Invoke(clonedDrop.m_itemData.m_shared, null);
            clonedDrop.m_itemData.m_dropPrefab = clonedPrefab; 
            clonedPrefab.name = newName;
            zns.m_namedPrefabs[newName.GetStableHashCode()] = clonedPrefab;
            ObjectDB.instance.m_items.Add(clonedPrefab);
            ObjectDB.instance.m_itemByHash[newName.GetStableHashCode()] = clonedPrefab;
            ObjectDB.instance.m_itemByData[clonedDrop.m_itemData.m_shared] = clonedPrefab;
            ClonedItems[newName] = (clonedPrefab, toClone.name, clonedDrop.m_itemData.m_shared);
            info.Apply(zns, clonedDrop);
        }
        
        ZNetScene zns = ZNetScene.instance;
        _DisabledForClonedItems.SetActive(false);
        if (!zns) return;
        Dictionary<string, ItemInfoWrapper.ItemInfo> newData = ValheimDB.ItemInfos.Value;
        List<ItemDrop> itemsOnGround = ItemDrop.s_instances;
        HashSet<string> obsoleteClonedItems = []; 
        foreach (KeyValuePair<string, (GameObject go, string from, ItemDrop.ItemData.SharedData sharedData)> item in ClonedItems.ToList())
        {
            if (newData.ContainsKey(item.Key) && newData[item.Key].CloneSource == item.Value.from) continue;
            obsoleteClonedItems.Add(item.Key);
            int hash = item.Key.GetStableHashCode();
            ObjectDB.instance.m_itemByHash.Remove(hash);
            ObjectDB.instance.m_itemByData.Remove(item.Value.sharedData);
            ObjectDB.instance.m_items.Remove(item.Value.go);
            zns.m_namedPrefabs.Remove(hash); 
            ClonedItems.Remove(item.Key);
        }
        InventoryHook.RemoveNonExisting(obsoleteClonedItems);
        for (int i = 0; i < itemsOnGround.Count; ++i)
        { 
            ItemDrop itemDrop = itemsOnGround[i];
            if (!itemDrop.m_nview.IsOwner()) continue;
            string prefab = itemDrop.m_itemData.m_dropPrefab.name; 
            if (obsoleteClonedItems.Contains(prefab)) ZNetScene.instance.Destroy(itemDrop.gameObject);
        }

        for (int i = _DisabledForClonedItems.transform.childCount - 1; i >= 0; --i)
        {
            if (!obsoleteClonedItems.Contains(_DisabledForClonedItems.transform.GetChild(i).gameObject.name)) continue;
            Object.Destroy(_DisabledForClonedItems.transform.GetChild(i).gameObject);
        }
        foreach (KeyValuePair<string, ItemInfoWrapper.ItemInfo> originalInfo in ModifiedItems)
        { 
            if (zns.GetPrefab(originalInfo.Key) is not GameObject prefab) continue;
            if (prefab.GetComponent<ItemDrop>() is not ItemDrop item) continue;
            originalInfo.Value.Apply(zns, item); 
        }
        List<ItemDrop.ItemData> inventories = InventoryHook.GetAlive();
        ApplyInventories(inventories, ModifiedItems);
        ApplyGroundItems(itemsOnGround, ModifiedItems);
        ModifiedItems.Clear();
        foreach (KeyValuePair<string, ItemInfoWrapper.ItemInfo> newInfo in newData.OrderByDescending(d => d.Value.CloneSource != null))
        {
            GameObject prefab = zns.GetPrefab(newInfo.Key);
            if (!prefab)
            {
                if (!string.IsNullOrWhiteSpace(newInfo.Value.CloneSource) && zns.GetPrefab(newInfo.Value.CloneSource) is {} toClone) CreateClonePrefab(zns, toClone, newInfo.Value, newInfo.Key);
                continue;
            }
            if (prefab.GetComponent<ItemDrop>() is not ItemDrop item) continue;
            ModifiedItems[newInfo.Key] = item;
            newInfo.Value.Apply(zns, item);
        } 
        ApplyInventories(inventories, newData);
        ApplyGroundItems(itemsOnGround, newData);
    }
    
    
    private static readonly Dictionary<string, MonsterInfoWrapper.MonsterInfo> ModifiedMonsters = [];
    private static readonly Dictionary<string, (GameObject go, string from)> ClonedMonsters = [];
    private static readonly GameObject _DisabledForClonedMonsters = new("ValheimDB_DisabledForClonedMonsters") { hideFlags = HideFlags.HideAndDontSave };
    public static void ApplyMonsters()
    {
        void CreateClonePrefab(ZNetScene zns, GameObject toClone, MonsterInfoWrapper.MonsterInfo info, string newName)
        {
            GameObject clonedPrefab = Object.Instantiate(toClone, _DisabledForClonedMonsters.transform);
            clonedPrefab.name = newName;
            zns.m_namedPrefabs[newName.GetStableHashCode()] = clonedPrefab;
            ClonedMonsters[newName] = (clonedPrefab, toClone.name);
            info.Apply(zns, clonedPrefab.GetComponent<Character>());
        }
        
        _DisabledForClonedMonsters.SetActive(false);
        ZNetScene zns = ZNetScene.instance;
        if (!zns) return;
        Dictionary<string, MonsterInfoWrapper.MonsterInfo> data = ValheimDB.MonsterInfos.Value;
        List<Character> characters = Character.s_characters;
        List<string> obsoleteClonedMonsters = [];
        foreach (KeyValuePair<string, (GameObject go, string from)> item in ClonedMonsters.ToList())
        {
            if (data.ContainsKey(item.Key) && data[item.Key].CloneSource == item.Value.from) continue;
            obsoleteClonedMonsters.Add(item.Key);
            int hash = item.Key.GetStableHashCode();
            zns.m_namedPrefabs.Remove(hash); 
            ClonedMonsters.Remove(item.Key);
        }
        for (int i = _DisabledForClonedMonsters.transform.childCount - 1; i >= 0; --i)
        {
            if (!obsoleteClonedMonsters.Contains(_DisabledForClonedMonsters.transform.GetChild(i).gameObject.name)) continue;
            Object.Destroy(_DisabledForClonedMonsters.transform.GetChild(i).gameObject);
        } 
        for(int i = 0; i < characters.Count; ++i)
        {
            Character character = characters[i];
            if (!character.m_nview.IsOwner()) continue;
            string prefabName = global::Utils.GetPrefabName(character.name);
            if (obsoleteClonedMonsters.Contains(prefabName)) ZNetScene.instance.Destroy(character.gameObject);
        }
        foreach (KeyValuePair<string, MonsterInfoWrapper.MonsterInfo> originalInfo in ModifiedMonsters)
        {
            if (zns.GetPrefab(originalInfo.Key) is not GameObject prefab) continue;
            if (prefab.GetComponent<Character>() is not Character character) continue;
            originalInfo.Value.Apply(zns, character);
        }
        for (int i = 0; i < characters.Count; ++i)
        {
            Character character = characters[i];
            string prefabName = global::Utils.GetPrefabName(character.name);
            if (!ModifiedMonsters.TryGetValue(prefabName, out MonsterInfoWrapper.MonsterInfo originalInfo)) continue;
            originalInfo.Apply(zns, character);
        } 
        ModifiedMonsters.Clear();
        foreach (KeyValuePair<string, MonsterInfoWrapper.MonsterInfo> newInfo in data.OrderByDescending(d => d.Value.CloneSource != null))
        {
            GameObject prefab = zns.GetPrefab(newInfo.Key);
            if (!prefab)
            {
                if (!string.IsNullOrWhiteSpace(newInfo.Value.CloneSource) && zns.GetPrefab(newInfo.Value.CloneSource) is {} toClone) CreateClonePrefab(zns, toClone, newInfo.Value, newInfo.Key);
                continue;
            }
            if (prefab.GetComponent<Character>() is not Character character) continue;
            ModifiedMonsters[newInfo.Key] = character;
            newInfo.Value.Apply(zns, character);
        }
        for (int i = 0; i < characters.Count; ++i)
        {
            Character character = characters[i];
            string prefabName = global::Utils.GetPrefabName(character.name);
            if (!data.TryGetValue(prefabName, out MonsterInfoWrapper.MonsterInfo newInfo)) continue;
            newInfo.Apply(zns, character);
        }
    }

    public static void ZNS_Awake_Postfix(ZNetScene __instance)
    {
        ApplyItems();
        ApplyPieces();
        ApplyMonsters();
    }
     
    [HarmonyPatch(typeof(Attack),nameof(Attack.Start))]
    private static class Attack_Start_Patch
    {
        private static void Postfix(Attack __instance, Humanoid character, bool __result)
        {
            if (!__result || !character) return;
            if (!ValheimDB.MonsterInfos.Value.TryGetValue(global::Utils.GetPrefabName(character.gameObject), out MonsterInfoWrapper.MonsterInfo newInfo) || !newInfo.DamageMultiplier.HasValue) return;
            __instance.m_damageMultiplier *= newInfo.DamageMultiplier.Value;
        }
    }
}