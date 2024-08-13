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
}
