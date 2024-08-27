using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;


public delegate void OnWorldResetDelegate();

/// <summary>
/// Generates and controls access to the Item Database and Inventory Data
/// </summary>
public class GameController : MonoBehaviour
{
    public List<ItemType> ItemTypes;
    public static event OnInventoryChangedDelegate OnInventoryChanged = delegate { };

    private static readonly List<OnWorldResetDelegate> OnWorldResetList = new();
    private static readonly Dictionary<string, ItemData> mItemDatabase = new();
    

    public static ItemData GetItemByGuid(string guid)
    {
        if (mItemDatabase.ContainsKey(guid))
        {
            return mItemDatabase[guid];
        }

        return null;
    }

    public static ItemData CreateItem(ItemType type)
    {
        ItemData item = type.CreateItem();
        mItemDatabase.Add(item.GUID, item);
        return item;
    }

    /// <returns>If could find and destroy item</returns>
    public static bool DestroyItemByGuid(string guid)
    {
        ItemData item = GetItemByGuid(guid);
        item.DestroyEntity();
        return mItemDatabase.Remove(guid);
    }

    public static void AddWorldResetListener(OnWorldResetDelegate listener)
    {
        OnWorldResetList.Add(listener);
    }

    public static void ResetWorld()
    {
        foreach (OnWorldResetDelegate listener in OnWorldResetList)
        {
            listener.Invoke();
        }
    }
}