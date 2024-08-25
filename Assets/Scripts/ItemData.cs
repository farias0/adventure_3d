using System;
using Unity.VisualScripting;
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
    public int MaxDurability; // If Degrades
    
    [SerializeField]
    private int mDurability;
    

    private void OnEnable()
    {
        mDurability = MaxDurability;
    }

    public int GetDurability()
    {
        return mDurability;
    }

    public void SetDurability(int value)
    {
        mDurability = value;
    }

    public ItemData Copy()
    {
        ItemData copy = CreateInstance<ItemData>();
        JsonUtility.FromJsonOverwrite(JsonUtility.ToJson(this), copy);
        return copy;
    }
}
