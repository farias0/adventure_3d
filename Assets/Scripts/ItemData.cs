using System;
using Unity.VisualScripting;
using UnityEngine;


[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item"), Serializable]
public class ItemData : ScriptableObject
{
    [Serializable]
    public class ItemDataRuntimeVars
    {
        // TODO maybe I should just separate the item asset from the runtime representation
        public int Durability;
    }


    public ItemDataRuntimeVars RuntimeVars = new();
    public string Name;
    public string GUID;
    public Sprite Icon;
    public GameObject Prefab;
    public bool CanDrop;
    public bool Degrades;
    [Range(1, 100)]
    public int MaxDurability; // If Degrades


    private void OnEnable()
    {
        RuntimeVars.Durability = MaxDurability;
    }

    public int GetDurability()
    {
        return RuntimeVars.Durability;
    }

    public void SetDurability(int value)
    {
        RuntimeVars.Durability = value;
    }

    public ItemData Copy()
    {
        ItemData copy = CreateInstance<ItemData>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), copy);
        return copy;
    }
}
