using System;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item"), Serializable]
public class ItemType : ScriptableObject
{
    public string Name;
    public string GUID;
    public Sprite Icon;
    public GameObject SpawnPrefab;
    public bool CanDrop;
    public bool IsUnique;
    public bool Degrades;
    [Range(1, 100)]
    public int MaxDurability; // If Degrades

    private bool mIsInstantiated;


    public void OnEnable()
    {
        mIsInstantiated = false;
    }

    public ItemData CreateItem()
    {
        if (IsUnique && mIsInstantiated)
        {
            Debug.LogError("Creating duplicate of unique item \"" + Name + "\"");
        }

        if (IsUnique) mIsInstantiated = true;
        
        return new(this);
    }
}
