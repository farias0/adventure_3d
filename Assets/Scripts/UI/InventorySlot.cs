using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Scripting;
using UnityEngine.UIElements;

public class InventorySlot : VisualElement
{
    private class BrokenOverlay : VisualElement
    {
        public BrokenOverlay()
        {
            AddToClassList("slotContainerBrokenOverlay");
        }
    }

    
    public Image Icon;
    public string ItemGuid = "";

    private readonly BrokenOverlay mBrokenOverlay = new();


    public void HoldItem(ItemData item)
    {
        Icon.image = item.Type.Icon.texture;
        ItemGuid = item.GUID;
    }

    public void DropItem()
    {
        ItemGuid = "";
        Icon.image = null;
        DisplayBrokenOverlay(false);
    }

    public void DisplayBrokenOverlay(bool active)
    {
        mBrokenOverlay.style.display = active ? DisplayStyle.Flex : DisplayStyle.None;
    }

    public InventorySlot()
    {
        //Create a new Image element and add it to the root
        Icon = new Image();
        Add(Icon);

        //Add USS style properties to the elements
        AddToClassList("slotContainer");
        Icon.AddToClassList("slotIcon");

        Add(mBrokenOverlay); 
        DisplayBrokenOverlay(false);

        RegisterCallback<PointerDownEvent>(OnPointerDown);
    }

    private void OnPointerDown(PointerDownEvent evt)
{
        //Not the left mouse button
        if (evt.button != 0 || ItemGuid.Equals(""))
        {
            return;
        }

        //Clear the image
        Icon.image = null;

        //Start the drag
        InventoryController.Instance.StartDrag(evt.position, this);
    }

    #region UXML
    [Preserve]
    public new class UxmlFactory : UxmlFactory<InventorySlot, UxmlTraits> { }

    [Preserve]
    public new class UxmlTraits : VisualElement.UxmlTraits { }
    #endregion
}
