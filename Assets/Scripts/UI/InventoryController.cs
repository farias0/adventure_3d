using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UIElements;

public class InventoryController : MonoBehaviour
{
    public List<InventorySlot> InventoryItems = new();

    private VisualElement mRoot;
    private VisualElement mSlotContainer;

    // Start is called before the first frame update
    void Start()
    {

    }

    // Update is called once per frame
    void Update()
    {
        
    }

    private void Awake()
    {
        //Store the root from the UI Document component
        mRoot = GetComponent<UIDocument>().rootVisualElement;

        //Search the root for the SlotContainer Visual Element
        mSlotContainer = mRoot.Q<VisualElement>("SlotContainer");

        //Create InventorySlots and add them as children to the SlotContainer
        for (int i = 0; i < 20; i++)
        {
            InventorySlot item = new();

            InventoryItems.Add(item);

            mSlotContainer.Add(item);
        }
    }
}
