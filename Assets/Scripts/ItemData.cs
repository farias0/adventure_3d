using System;
using UnityEngine;


[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item"), Serializable]
public class ItemData : ScriptableObject
{
    public string Name;
    public string GUID;
    public Sprite Icon;
    public GameObject Prefab;
    public bool CanDrop;
    public bool Degrades;
    [Range(1, 100)]
    public int Durability; // If Degrades

    public ItemData Copy()
    {
        ItemData copy = CreateInstance<ItemData>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), copy);
        return copy;
    }
}
