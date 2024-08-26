#nullable enable

using System;
using Unity.VisualScripting;
using UnityEngine;

public class ItemData
{
    public readonly ItemType Type;
    public readonly string Name;
    public readonly string GUID;
    public ItemEntity? Entity;
    public int Durability;


    public ItemData(ItemType type)
    {
        Type = type;
        Name = type.Name;
        GUID = Guid.NewGuid().ToString();
        ResetDurability();
    }

    public void ResetDurability()
    {
        Durability = Type.MaxDurability;
    }

    public void DestroyEntity()
    {
        if (Entity != null)
        {
            Entity.ClearItem();
            GameObject.Destroy(Entity.gameObject);
        }
    }
}
