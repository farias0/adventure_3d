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
    private static readonly Dictionary<string, ItemEntity> mItemDatabase = new();

    /// <summary>
    /// Retrieve item details based on the GUID
    /// </summary>
    /// <param name="guid">ID to look up</param>
    /// <returns>Item details</returns>
    public static ItemEntity GetItemByGuid(string guid)
    {
        if (mItemDatabase.ContainsKey(guid))
        {
            return mItemDatabase[guid];
        }

        return null;
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


    private void Awake()
    {
        foreach (ItemType type in ItemTypes)
        {
            ItemEntity item = type.CreateEntity();
            mItemDatabase.Add(item.GUID, item);
        }
    }

    private void Start()
    {
        List<ItemEntity> mStartingInventory = new();
        mStartingInventory.AddRange(mItemDatabase.Values);
        OnInventoryChanged.Invoke(mStartingInventory.Select(x=> x.GUID).ToArray(), InventoryChangeType.Pickup);
    }
}