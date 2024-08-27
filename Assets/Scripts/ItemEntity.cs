#nullable enable

using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class ItemEntity : MonoBehaviour
{
    private enum State
    {
        InWorld,
        EquippedByPlayer
    }

    public ItemType? Type;

    private ItemData? mItem;
    private State mState = State.InWorld;


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
        if (mState == State.InWorld && Player.Instance.InteractedWithMeThisFrame(transform.position))
        {
            if (MoveToInventory()) return;
        }
    }

    public void PrepareForDestroy() // Since OnDestroy() isn't called immediately...
    {
        if (mItem?.Entity == this) mItem.Entity = null;

        else if (mItem?.Entity != this)
        {
            Debug.Log("ItemEntity.PrepareForDestroy: item.Entity is referencing a different ItemEntity.\n" +
                           "Make sure an ItemEntity just spawned, otherwise the item will contain an invalid reference.");
        }
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
            Debug.LogError("ItemEntity.InstantiatedFromItem: provided item is null");
            return;
        }

        if (mItem != null)
        {
            Debug.LogError("ItemEntity.InstantiatedFromItem: entity already has an item");
            return;
        }

        if (item.Entity != null)
        {
            Debug.Log("ItemEntity.InstantiatedFromItem: item already has an ItemEntity reference.\n" +
                      "Make sure an ItemEntity is being destroyed, because the item's entity reference is going to change.");
        }
        
        SetItem(item);
    }

    public void EquippedByPlayer()
    {
        mState = State.EquippedByPlayer;
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

    private bool MoveToInventory()
    {
        bool success = false;

        if (InventoryController.Instance!.AddItem(mItem!))
        {
            PrepareForDestroy();
            Destroy(gameObject);
            success = true;
        }

        return success;
    }
}
