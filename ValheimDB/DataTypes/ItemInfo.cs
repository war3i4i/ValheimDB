using System;
using System.Collections.Generic;
using UnityEngine;
using YamlDotNet.Serialization;
using static ValheimDB.Utils;

namespace ValheimDB.DataTypes;

public class ItemInfoWrapper
{
    [Flags]
    private enum ItemInfoFlags
    {
        None = 0,
        HasName = 1 << 0,
        HasDescription = 1 << 1,
        HasCraftingStation = 1 << 2,
        HasWeight = 1 << 3,
        HasMaxDurability = 1 << 4,
        HasRepairStationLevel = 1 << 5,
        HasCraft = 1 << 6,
        HasDamage = 1 << 7,
        HasDamagePerLevel = 1 << 8,
        HasCloneSource = 1 << 9
    }
    
    private static bool HasFlagFast(ItemInfoFlags value, ItemInfoFlags flag) => (value & flag) != 0;
    
    public class ItemDamage
    {
        public int Blunt;
        public int Slash;
        public int Pierce;
        public int Chop;
        public int Pickaxe;
        public int Fire;
        public int Frost;
        public int Lightning;
        public int Poison;
        public int Spirit;
    }

    public class ItemInfo : ISerializableParameter
    {
        [YamlIgnore] public bool _hasRecipe = true;
        [YamlIgnore] public bool _hasCraftingStation = true;

        [DbAlias("m_name")] public string Name;
        [DbAlias("m_description")] public string Description;
        [DbAlias("m_craftingStation", "craftingStation")] public string CraftingStation;
        [DbAlias("m_weight")] public int? Weight;
        [DbAlias("m_maxDurability")] public int? MaxDurability;
        [DbAlias("minStationLevel")] public int? RepairStationLevel;
        [DbAlias("Build", "reqs")] public List<string> Craft;
        [DbAlias("Damages")] public ItemDamage? Damage;
        [DbAlias("Damage_Per_Level")] public ItemDamage? DamagePerLevel;
        [DbAlias("clonePrefabName")] public string CloneSource;

        public void Serialize(ref ZPackage pkg)
        {
            ItemInfoFlags flags = ItemInfoFlags.None;

            if (!string.IsNullOrEmpty(Name)) flags |= ItemInfoFlags.HasName;
            if (!string.IsNullOrEmpty(Description)) flags |= ItemInfoFlags.HasDescription;
            if (!string.IsNullOrEmpty(CraftingStation)) flags |= ItemInfoFlags.HasCraftingStation;
            if (Weight.HasValue) flags |= ItemInfoFlags.HasWeight;
            if (MaxDurability.HasValue) flags |= ItemInfoFlags.HasMaxDurability;
            if (RepairStationLevel.HasValue) flags |= ItemInfoFlags.HasRepairStationLevel;
            if (Craft != null) flags |= ItemInfoFlags.HasCraft;
            if (Damage != null) flags |= ItemInfoFlags.HasDamage;
            if (DamagePerLevel != null) flags |= ItemInfoFlags.HasDamagePerLevel;
            if (!string.IsNullOrEmpty(CloneSource)) flags |= ItemInfoFlags.HasCloneSource;

            pkg.Write((int)flags);

            if (HasFlagFast(flags, ItemInfoFlags.HasName)) pkg.Write(Name);
            if (HasFlagFast(flags, ItemInfoFlags.HasDescription)) pkg.Write(Description);
            if (HasFlagFast(flags, ItemInfoFlags.HasCraftingStation)) pkg.Write(CraftingStation);
            if (HasFlagFast(flags, ItemInfoFlags.HasWeight)) pkg.Write(Weight.Value);
            if (HasFlagFast(flags, ItemInfoFlags.HasMaxDurability)) pkg.Write(MaxDurability.Value);
            if (HasFlagFast(flags, ItemInfoFlags.HasRepairStationLevel)) pkg.Write(RepairStationLevel.Value);
            if (HasFlagFast(flags, ItemInfoFlags.HasCloneSource)) pkg.Write(CloneSource);

            if (HasFlagFast(flags, ItemInfoFlags.HasCraft))
            {
                pkg.Write(Craft.Count);
                for (int i = 0; i < Craft.Count; ++i) pkg.Write(Craft[i]);
            }

            if (HasFlagFast(flags, ItemInfoFlags.HasDamage))
            {
                pkg.Write(Damage.Blunt);
                pkg.Write(Damage.Slash);
                pkg.Write(Damage.Pierce);
                pkg.Write(Damage.Chop);
                pkg.Write(Damage.Pickaxe);
                pkg.Write(Damage.Fire);
                pkg.Write(Damage.Frost);
                pkg.Write(Damage.Lightning);
                pkg.Write(Damage.Poison);
                pkg.Write(Damage.Spirit);
            }

            if (HasFlagFast(flags, ItemInfoFlags.HasDamagePerLevel))
            {
                pkg.Write(DamagePerLevel.Blunt);
                pkg.Write(DamagePerLevel.Slash);
                pkg.Write(DamagePerLevel.Pierce);
                pkg.Write(DamagePerLevel.Chop);
                pkg.Write(DamagePerLevel.Pickaxe);
                pkg.Write(DamagePerLevel.Fire);
                pkg.Write(DamagePerLevel.Frost);
                pkg.Write(DamagePerLevel.Lightning);
                pkg.Write(DamagePerLevel.Poison);
                pkg.Write(DamagePerLevel.Spirit);
            }
        }

        public void Deserialize(ref ZPackage pkg)
        {
            ItemInfoFlags flags = (ItemInfoFlags)pkg.ReadInt();

            if (HasFlagFast(flags, ItemInfoFlags.HasName)) Name = pkg.ReadString();
            if (HasFlagFast(flags, ItemInfoFlags.HasDescription)) Description = pkg.ReadString();
            if (HasFlagFast(flags, ItemInfoFlags.HasCraftingStation)) CraftingStation = pkg.ReadString();
            if (HasFlagFast(flags, ItemInfoFlags.HasWeight)) Weight = pkg.ReadInt();
            if (HasFlagFast(flags, ItemInfoFlags.HasMaxDurability)) MaxDurability = pkg.ReadInt();
            if (HasFlagFast(flags, ItemInfoFlags.HasRepairStationLevel)) RepairStationLevel = pkg.ReadInt();
            if (HasFlagFast(flags, ItemInfoFlags.HasCloneSource)) CloneSource = pkg.ReadString();

            if (HasFlagFast(flags, ItemInfoFlags.HasCraft))
            {
                int craftCount = pkg.ReadInt();
                Craft = new List<string>(craftCount);
                for (int i = 0; i < craftCount; ++i) Craft.Add(pkg.ReadString());
            }

            if (HasFlagFast(flags, ItemInfoFlags.HasDamage))
                Damage = new ItemDamage
                {
                    Blunt = pkg.ReadInt(),
                    Slash = pkg.ReadInt(),
                    Pierce = pkg.ReadInt(),
                    Chop = pkg.ReadInt(),
                    Pickaxe = pkg.ReadInt(),
                    Fire = pkg.ReadInt(),
                    Frost = pkg.ReadInt(),
                    Lightning = pkg.ReadInt(),
                    Poison = pkg.ReadInt(),
                    Spirit = pkg.ReadInt()
                };

            if (HasFlagFast(flags, ItemInfoFlags.HasDamagePerLevel))
                DamagePerLevel = new ItemDamage
                {
                    Blunt = pkg.ReadInt(),
                    Slash = pkg.ReadInt(),
                    Pierce = pkg.ReadInt(),
                    Chop = pkg.ReadInt(),
                    Pickaxe = pkg.ReadInt(),
                    Fire = pkg.ReadInt(),
                    Frost = pkg.ReadInt(),
                    Lightning = pkg.ReadInt(),
                    Poison = pkg.ReadInt(),
                    Spirit = pkg.ReadInt()
                };
        }

        public static implicit operator ItemInfo(ItemDrop item)
        {
            ItemInfo newInfo = new ItemInfo
            {
                Name = item.m_itemData.m_shared.m_name,
                Description = item.m_itemData.m_shared.m_description,
                Weight = (int)item.m_itemData.m_shared.m_weight,
                MaxDurability = (int)item.m_itemData.m_shared.m_maxDurability,
            };
            Recipe r = ObjectDB.instance.GetRecipe(item.m_itemData);
            if (r == null) newInfo._hasRecipe = false;
            else
            {
                List<string> craftList = new List<string>(r.m_resources.Length);
                for (int i = 0; i < r.m_resources.Length; ++i)
                {
                    Piece.Requirement req = r.m_resources[i];
                    string reqString = $"{req.m_resItem.gameObject.name}:{req.m_amount}:{req.m_recover}";
                    craftList.Add(reqString);
                }
                newInfo.CraftingStation = r.m_craftingStation?.name ?? null;
                newInfo.RepairStationLevel = r.m_minStationLevel;
                newInfo.Craft = craftList;
                newInfo._hasCraftingStation = r.m_craftingStation != null;
            }

            newInfo.Damage = new ItemDamage
            {
                Blunt = (int)item.m_itemData.m_shared.m_damages.m_blunt,
                Slash = (int)item.m_itemData.m_shared.m_damages.m_slash,
                Pierce = (int)item.m_itemData.m_shared.m_damages.m_pierce,
                Chop = (int)item.m_itemData.m_shared.m_damages.m_chop,
                Pickaxe = (int)item.m_itemData.m_shared.m_damages.m_pickaxe,
                Fire = (int)item.m_itemData.m_shared.m_damages.m_fire,
                Frost = (int)item.m_itemData.m_shared.m_damages.m_frost,
                Lightning = (int)item.m_itemData.m_shared.m_damages.m_lightning,
                Poison = (int)item.m_itemData.m_shared.m_damages.m_poison,
                Spirit = (int)item.m_itemData.m_shared.m_damages.m_spirit
            };
            newInfo.DamagePerLevel = new ItemDamage
            {
                Blunt = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_blunt,
                Slash = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_slash,
                Pierce = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_pierce,
                Chop = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_chop,
                Pickaxe = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_pickaxe,
                Fire = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_fire,
                Frost = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_frost,
                Lightning = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_lightning,
                Poison = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_poison,
                Spirit = (int)item.m_itemData.m_shared.m_damagesPerLevel.m_spirit
            };
            return newInfo;
        }

        public void Apply(ZNetScene zns, ItemDrop item)
        {
            Apply(item.m_itemData.m_shared);
            Recipe recipe = ObjectDB.instance.GetRecipe(item.m_itemData);
            if (Craft != null && Craft.Count > 0)
            {
                List<Piece.Requirement> reqs = new List<Piece.Requirement>(Craft.Count);
                for (int i = 0; i < Craft.Count; ++i)
                {
                    string req = Craft[i]; 
                    string[] split = req.Split(':');
                    string prefab = split[0];
                    GameObject obj = zns.GetPrefab(prefab);
                    if (obj == null) continue;
                    reqs.Add(new Piece.Requirement
                    {
                        m_resItem = obj.GetComponent<ItemDrop>(),
                        m_amount = int.Parse(split[1]),
                        m_recover = split.Length > 2 && bool.Parse(split[2])
                    });
                }
                if (recipe == null)
                {
                    recipe = ScriptableObject.CreateInstance<Recipe>();
                    recipe.name = $"{item.name}_Recipe";
                    recipe.m_item = item;
                    ObjectDB.instance.m_recipes.Add(recipe);
                }
                recipe.m_resources = reqs.ToArray();
                if (_hasCraftingStation) {
                    if (!string.IsNullOrEmpty(CraftingStation))
                        recipe.m_craftingStation = zns.GetPrefab(CraftingStation)?.GetComponent<CraftingStation>();
                } else recipe.m_craftingStation = null;
                if (RepairStationLevel.HasValue) recipe.m_minStationLevel = RepairStationLevel.Value;
            }
            else
            {
                if (!_hasRecipe)
                {
                    if (recipe != null) ObjectDB.instance.m_recipes.Remove(recipe);
                }
            }
        }
        
        public void Apply(ItemDrop.ItemData.SharedData shared)
        {
            if (!string.IsNullOrEmpty(Name)) shared.m_name = Name;
            if (!string.IsNullOrEmpty(Description)) shared.m_description = Description;
            if (Weight.HasValue) shared.m_weight = Weight.Value;
            if (MaxDurability.HasValue) shared.m_maxDurability = MaxDurability.Value;
            if (Damage != null)
            { 
                HitData.DamageTypes dmg = shared.m_damages;
                dmg.m_blunt = Damage.Blunt;
                dmg.m_slash = Damage.Slash;
                dmg.m_pierce = Damage.Pierce;
                dmg.m_chop = Damage.Chop;
                dmg.m_pickaxe = Damage.Pickaxe;
                dmg.m_fire = Damage.Fire;
                dmg.m_frost = Damage.Frost;
                dmg.m_lightning = Damage.Lightning;
                dmg.m_poison = Damage.Poison;
                dmg.m_spirit = Damage.Spirit;
            } 
            if (DamagePerLevel != null)
            {
                HitData.DamageTypes dmgLvl = shared.m_damagesPerLevel;
                dmgLvl.m_blunt = DamagePerLevel.Blunt;
                dmgLvl.m_slash = DamagePerLevel.Slash;
                dmgLvl.m_pierce = DamagePerLevel.Pierce;
                dmgLvl.m_chop = DamagePerLevel.Chop;
                dmgLvl.m_pickaxe = DamagePerLevel.Pickaxe;
                dmgLvl.m_fire = DamagePerLevel.Fire;
                dmgLvl.m_frost = DamagePerLevel.Frost;
                dmgLvl.m_lightning = DamagePerLevel.Lightning;
                dmgLvl.m_poison = DamagePerLevel.Poison;
                dmgLvl.m_spirit = DamagePerLevel.Spirit;
            }
        }
    }
}