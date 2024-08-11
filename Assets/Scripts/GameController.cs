using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

using UnityEngine;

[Serializable]
public class ItemDetails
{
    public string Name;
    public string GUID;
    public Sprite Icon;
    public bool CanDrop;
}

public enum InventoryChangeType
{
    Pickup,
    Drop
}
public delegate void OnInventoryChangedDelegate(string[] itemGuid, InventoryChangeType change);

/// <summary>
/// Generates and controls access to the Item Database and Inventory Data
/// </summary>
public class GameController : MonoBehaviour
{
    [SerializeField]
    public List<Sprite> IconSprites;
    public static event OnInventoryChangedDelegate OnInventoryChanged = delegate { };

    private static readonly Dictionary<string, ItemDetails> mItemDatabase = new();
    private readonly List<ItemDetails> mPlayerInventory = new();


    /// <summary>
    /// Retrieve item details based on the GUID
    /// </summary>
    /// <param name="guid">ID to look up</param>
    /// <returns>Item details</returns>
    public static ItemDetails GetItemByGuid(string guid)
    {
        if (mItemDatabase.ContainsKey(guid))
        {
            return mItemDatabase[guid];
        }

        return null;
    }


    private void Awake()
    {
        PopulateDatabase();
    }

    private void Start()
    {
        //Add the ItemDatabase to the players inventory and let the UI know that some items have been picked up
        mPlayerInventory.AddRange(mItemDatabase.Values);
        OnInventoryChanged.Invoke(mPlayerInventory.Select(x=> x.GUID).ToArray(), InventoryChangeType.Pickup);
    }

    /// <summary>
    /// Populate the database
    /// </summary>
    private void PopulateDatabase()
    {
        mItemDatabase.Add("8B0EF21A-F2D9-4E6F-8B79-031CA9E202BA", new ItemDetails()
        {
            Name = "Sword Default",
            GUID = "8B0EF21A-F2D9-4E6F-8B79-031CA9E202BA",
            Icon = IconSprites.FirstOrDefault(x => x.name.Equals("SwordDefault")),
            CanDrop = true
        });

        mItemDatabase.Add("992D3386-B743-4CD3-9BB7-0234A057C265", new ItemDetails()
        {
            Name = "Shield Default",
            GUID = "992D3386-B743-4CD3-9BB7-0234A057C265",
            Icon = IconSprites.FirstOrDefault(x => x.name.Equals("ShieldDefault")),
            CanDrop = false
        });

    }
}