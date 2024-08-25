using System;
using Unity.VisualScripting;
using UnityEngine;

public class ItemEntity
{
    public readonly ItemType Type;
    public readonly string Name;
    public readonly string GUID;
    public int Durability;


    public ItemEntity(ItemType type)
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
}
