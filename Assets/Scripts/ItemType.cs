using System;
using Unity.VisualScripting;
using UnityEngine;

[CreateAssetMenu(fileName = "NewItem", menuName = "Inventory/Item"), Serializable]
public class ItemType : ScriptableObject
{
    public string Name;
    public string GUID;
    public Sprite Icon;
    public GameObject Prefab;
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

    public ItemEntity CreateEntity()
    {
        if (IsUnique && mIsInstantiated)
        {
            Debug.LogWarning("Tried to reinstantiate item [" + Name + "].");
            return null;
        }

        if (IsUnique) mIsInstantiated = true;
        
        return new(this);
    }
}
