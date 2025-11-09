using System;
using System.CodeDom;
using System.Collections.Generic;
using HarmonyLib;
using UnityEngine;
 
namespace ValheimDB;

public class InventoryHook
{
    private static readonly List<WeakReference<Inventory>> AllInventories = [];
    [HarmonyPatch(typeof(Inventory), MethodType.Constructor, typeof(string), typeof(Sprite), typeof(int), typeof(int))]
    private static class Inventory_ctor_Patch { private static void Postfix(Inventory __instance) => AllInventories.Add(new WeakReference<Inventory>(__instance)); }
    public static List<ItemDrop.ItemData> GetAlive()
    {
        AllInventories.RemoveAll(wr => !wr.TryGetTarget(out _));
        List<ItemDrop.ItemData> sharedDatas = [];
        foreach (WeakReference<Inventory> wr in AllInventories)
        {
            if (wr.TryGetTarget(out Inventory inventory))
            {
                foreach (ItemDrop.ItemData itemData in inventory.GetAllItems()) sharedDatas.Add(itemData);
            }
        }
        return sharedDatas;
    }
    public static void RemoveNonExisting(HashSet<string> keys)
    {
        AllInventories.RemoveAll(wr => !wr.TryGetTarget(out _));
        foreach (WeakReference<Inventory> wr in AllInventories)
        {
            if (wr.TryGetTarget(out Inventory inventory))
            {
                List<ItemDrop.ItemData> itemsToRemove = [];
                foreach (ItemDrop.ItemData itemData in inventory.GetAllItems())
                {
                    if (!itemData.m_dropPrefab) continue; 
                    string key = itemData.m_dropPrefab.name;
                    if (keys.Contains(key)) itemsToRemove.Add(itemData);
                }
                foreach (ItemDrop.ItemData itemData in itemsToRemove) inventory.RemoveItem(itemData);
            }
        }
    }
}