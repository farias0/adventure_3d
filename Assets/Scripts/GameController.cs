using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;
using UnityEngine.AI;


/// <summary>
/// Generates and controls access to the Item Database and Inventory Data
/// </summary>
public class GameController : MonoBehaviour
{
    public List<ItemData> Items;
    public static event OnInventoryChangedDelegate OnInventoryChanged = delegate { };

    private static readonly Dictionary<string, ItemData> mItemDatabase = new();

    /// <summary>
    /// Retrieve item details based on the GUID
    /// </summary>
    /// <param name="guid">ID to look up</param>
    /// <returns>Item details</returns>
    public static ItemData GetItemByGuid(string guid)
    {
        if (mItemDatabase.ContainsKey(guid))
        {
            return mItemDatabase[guid];
        }

        return null;
    }


    private void Awake()
    {
        foreach (ItemData item in Items)
        {
            mItemDatabase.Add(item.GUID, item);
        }
    }

    private void Start()
    {
        List<ItemData> mStartingInventory = new();
        mStartingInventory.AddRange(mItemDatabase.Values);
        OnInventoryChanged.Invoke(mStartingInventory.Select(x=> x.GUID).ToArray(), InventoryChangeType.Pickup);
    }
}