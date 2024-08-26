#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEntity : MonoBehaviour
{
    public ItemType? Type;

    private ItemData? mItem;


    // Start is called before the first frame update
    void Start()
    {
        if (Type == null)
        {
            Debug.LogError("ItemEntity has no type set");
            return;
        }

        if (mItem == null) // Created from a prefab in the world
        {
            SetItem(GameController.CreateItem(Type));
        }
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnDestroy()
    {
        if (mItem != null) DestroyItem();
    }

    /// <summary>
    /// Create an ItemEntity from an existing ItemData.
    /// Should be called immediately after instantiating the entity,
    /// meaning before Start() is called.
    /// </summary>
    /// <param name="item"></param>
    public void InstantiatedFromItem(ItemData item)
    {
        if (item == null)
        {
            Debug.LogError("ItemEntity.SetItem: provided item is null");
            return;
        }

        if (item.Entity != null)
        {
            Debug.LogError("ItemEntity.SetItem: item already has an entity");
            return;
        }

        if (mItem != null)
        {
            Debug.LogError("ItemEntity.SetItem: entity already has an item");
            return;
        }
        
        SetItem(item);
    }

    // Removes the item reference
    public void ClearItem()
    {
        mItem = null;
    }

    private void SetItem(ItemData item)
    {
        mItem = item;
        item.Entity = this;
    }

    private void DestroyItem()
    {
        if (mItem != null)
        {
            mItem!.Entity = null;
            GameController.DestroyItemByGuid(mItem!.GUID);
        }
    }
}
