using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;

public enum InventoryChangeType
{
    Pickup,
    Drop
}

public delegate void OnInventoryChangedDelegate(string[] itemGuid, InventoryChangeType change);
public delegate void OnPlayerEquippedItemChangedDelegate(string itemGuid, InventoryChangeType change);

class SelectedSlotManager
{
    public enum CursorDirection
    {
        Left,
        Right,
        Up,
        Down
    }


    readonly int mSlotCount;
    private int mSelectedSlotIndex = 0;

    private const int EquipmentSlotsCount = 2;
    private const int SlotsPerRow = 5;


    public SelectedSlotManager(int slotCount)
    {
        mSlotCount = slotCount;
    }

    public int GetSelectedSlotIndex() => mSelectedSlotIndex;

    public void SetSelectedSlot(int index)
    {
        mSelectedSlotIndex = index;
    }

    public void MoveCursor(CursorDirection dir)
    {
        switch (dir)
        {
            case CursorDirection.Left:
                if (mSelectedSlotIndex == 0) return;
                if ((mSelectedSlotIndex > EquipmentSlotsCount) && (mSelectedSlotIndex - EquipmentSlotsCount) % SlotsPerRow == 0) {
                    // Left edge of the inventory panel
                    mSelectedSlotIndex = EquipmentSlotsCount - 1;
                    return;
                }
                mSelectedSlotIndex--;
                if (mSelectedSlotIndex < 0) mSelectedSlotIndex = mSlotCount - 1;
                break;

            case CursorDirection.Right:
                if (mSelectedSlotIndex == mSlotCount - 1) return;
                mSelectedSlotIndex++;
                if (mSelectedSlotIndex >= mSlotCount) mSelectedSlotIndex = 0;
                break;

            case CursorDirection.Up:
                if (mSelectedSlotIndex < EquipmentSlotsCount) return; // Equipment panel
                mSelectedSlotIndex -= SlotsPerRow;
                if (mSelectedSlotIndex < EquipmentSlotsCount) mSelectedSlotIndex -= EquipmentSlotsCount; // Can't finish on the equipment panel
                if (mSelectedSlotIndex < 0) mSelectedSlotIndex += mSlotCount;
                break;

            case CursorDirection.Down:
                if (mSelectedSlotIndex < EquipmentSlotsCount) return; // Equipment panel
                mSelectedSlotIndex += SlotsPerRow;
                if (mSelectedSlotIndex >= mSlotCount) mSelectedSlotIndex -= mSlotCount - EquipmentSlotsCount;
                break;
        }
    }
}

public class InventoryController : MonoBehaviour
{
    public List<Sprite> SelectedSlotAnimFrames;


    /// <summary>
    /// There should only be a single instance per scene
    /// </summary>
    public static InventoryController Instance { get; private set; }

    public static event OnPlayerEquippedItemChangedDelegate OnPlayerWeaponChanged = delegate { };
    public static event OnPlayerEquippedItemChangedDelegate OnPlayerShieldChanged = delegate { };


    private VisualElement mRoot;
    private VisualElement mEquipmentSlotContainer;
    private VisualElement mInventorySlotContainer;
    private readonly List<InventorySlot> InventorySlots = new();
    private StyleBackground mSlotDefaultBG;
    private VisualElement mGhostIcon;
    private SelectedSlotManager mSelectedSlotManager;
    private AnimationSelectedItemSlot mSelectedSlotAnimation;
    private bool mIsInventoryOpen;
    private bool mIsDragging;
    private InventorySlot mOriginalSlot; // Used when moving an item between slots
    private SelectedSlotManager.CursorDirection? mLastCursorDirection = null; // The last directional input from the player


    private const float GamepadDeadzone = 0.25f; 
    private const int EquipmentSlotsCount = 2;


    public void StartDrag(Vector2 cursorPosition, InventorySlot fromSlot)
    {
        mIsDragging = true;
        mOriginalSlot = fromSlot;

        EnableGhostIcon(GameController.GetItemByGuid(mOriginalSlot.ItemGuid).Icon);
        SyncGhostIconWithPosition(cursorPosition);

        SetSelectedSlot(InventorySlots.IndexOf(fromSlot));
    }

    public bool IsOpen()
    {
        return mIsInventoryOpen;
    }

    public ItemData GetEquippedWeapon()
    {
        return GameController.GetItemByGuid(InventorySlots[0].ItemGuid);
    }

    public ItemData GetEquippedShield()
    {
        return GameController.GetItemByGuid(InventorySlots[1].ItemGuid);
    }

    // Start is called before the first frame update
    void Start()
    {
        mSelectedSlotManager = new SelectedSlotManager(mEquipmentSlotContainer.childCount + mInventorySlotContainer.childCount);
        mSelectedSlotAnimation = new AnimationSelectedItemSlot(SelectedSlotAnimFrames);
        mSlotDefaultBG = mInventorySlotContainer[0].style.backgroundImage;
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInput();

        if (!mIsInventoryOpen) return;

        int selectedSlotIndex = mSelectedSlotManager.GetSelectedSlotIndex();
        GetSlotElementByIndex(selectedSlotIndex).style.backgroundImage = mSelectedSlotAnimation.TickAnimation().texture;
    }

    private void Awake()
    {
        Instance = this;

        //Store the root from the UI Document component
        mRoot = GetComponent<UIDocument>().rootVisualElement;

        mGhostIcon = mRoot.Query<VisualElement>("GhostIcon");

        mEquipmentSlotContainer = mRoot.Q<VisualElement>("EquipmentSlotContainer");
        mInventorySlotContainer = mRoot.Q<VisualElement>("InventorySlotContainer");
        
        InventorySlot weapon = new();
        InventorySlots.Add(weapon);
        mEquipmentSlotContainer.Add(weapon);

        InventorySlot shield = new();
        InventorySlots.Add(shield);
        mEquipmentSlotContainer.Add(shield);

        for (int i = 0; i < 20; i++)
        {
            InventorySlot item = new();

            InventorySlots.Add(item);

            mInventorySlotContainer.Add(item);
        }

        GameController.OnInventoryChanged += GameController_OnInventoryChanged;

        mGhostIcon.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        mGhostIcon.RegisterCallback<PointerUpEvent>(OnPointerUp);

        //A little gambiarra -- the inventory starts closed!
        mIsInventoryOpen = true;
        ToggleInventory();
    }

    private void GameController_OnInventoryChanged(string[] itemGuid, InventoryChangeType change)
    {
        //Loop through each item and if it has been picked up, add it to the next empty slot
        foreach (string item in itemGuid)
        {
            if (change == InventoryChangeType.Pickup)
            {
                var emptySlot = InventorySlots
                                        .Skip(EquipmentSlotsCount)
                                        .FirstOrDefault(x => x.ItemGuid.Equals(""));
                            
                emptySlot?.HoldItem(GameController.GetItemByGuid(item));
            }
        }
    }

    private void ToggleInventory()
    {
        // Toggle visibility of the inventory
        mIsInventoryOpen = !mIsInventoryOpen;
        mRoot.style.display = mIsInventoryOpen ? DisplayStyle.Flex : DisplayStyle.None;

        if (mIsInventoryOpen)
        {
            FadeIn(mRoot, 250);
            SetSelectedSlot(EquipmentSlotsCount);
        }
        else
            mSelectedSlotAnimation?.ResetAnimation();

        // Lock or unlock the cursor
        // Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        // Cursor.visible = isInventoryOpen;

        // Handle pausing/unpausing the game
        // Time.timeScale = isInventoryOpen ? 0f : 1f;
    }

    private VisualElement GetSlotElementByIndex(int index)
    {
        if (index < EquipmentSlotsCount) return mEquipmentSlotContainer[index];
        else return mInventorySlotContainer[index - EquipmentSlotsCount];
    }

    private void SetSelectedSlot(int index)
    {
        StopAnimatingSlot(mSelectedSlotManager.GetSelectedSlotIndex());
        mSelectedSlotManager.SetSelectedSlot(index);
    }

    private void ProcessInput()
    {

        if (Input.GetButtonDown("ToggleInventory")) ToggleInventory();

        if (!mIsInventoryOpen || mIsDragging) return;


        float moveX = Input.GetAxis("Horizontal");
        float moveZ = Input.GetAxis("Vertical");

        // For keyboards
        if ((Math.Abs(moveX) == 1) && (Math.Abs(moveZ) == 1))
        {
            moveX *= 0.7071f;
            moveZ *= 0.7071f;
        }

        SelectedSlotManager.CursorDirection? cursorDirection = null;

        if (moveX > GamepadDeadzone)
            cursorDirection = SelectedSlotManager.CursorDirection.Right;
        else if (moveX < -1 * GamepadDeadzone)
            cursorDirection = SelectedSlotManager.CursorDirection.Left;
        else if (moveZ > GamepadDeadzone)
            cursorDirection = SelectedSlotManager.CursorDirection.Up;
        else if (moveZ < -1 * GamepadDeadzone)
            cursorDirection = SelectedSlotManager.CursorDirection.Down;
        
        // Disables hold to keep moving cursor
        if (cursorDirection != null && cursorDirection != mLastCursorDirection)
        {
            StopAnimatingSlot(mSelectedSlotManager.GetSelectedSlotIndex());
            mSelectedSlotManager.MoveCursor(cursorDirection.Value);
            
            if (mOriginalSlot != null)
                SyncGhostIconWithSelectedSlot();
        }

        mLastCursorDirection = cursorDirection;


        if (Input.GetButtonDown("Submit"))
        {
            InventorySlot selectedSlot = InventorySlots[mSelectedSlotManager.GetSelectedSlotIndex()];
            if (mOriginalSlot == null) StartMovingItem(selectedSlot);
            else FinishMovingItem(selectedSlot);
        }
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        if (!mIsDragging) return;

        SyncGhostIconWithPosition(evt.position);
    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!mIsDragging) return;


        //Check to see if they are dropping the ghost icon over any inventory slots.
        IEnumerable<InventorySlot> slots = InventorySlots.Where(x => x.worldBound.Overlaps(mGhostIcon.worldBound));

        //Found at least one
        if (slots.Count() != 0)
        {
            InventorySlot closestSlot = slots.OrderBy(x =>
                            Vector2.Distance(x.worldBound.position, mGhostIcon.worldBound.position))
                        .First();
            
            MoveItemToSlot(mOriginalSlot, closestSlot);
        }
        //Didn't find any (dragged off the window)
        else
        {
            mOriginalSlot.Icon.image = GameController.GetItemByGuid(mOriginalSlot.ItemGuid).Icon.texture;
        }

        //Clear dragging related visuals and data
        mIsDragging = false;
        mOriginalSlot = null;
        mGhostIcon.style.visibility = Visibility.Hidden;

    }

    private void StartMovingItem(InventorySlot fromSlot)
    {
        if (fromSlot.ItemGuid == "") return;

        mOriginalSlot = fromSlot;
        fromSlot.Icon.image = null;
        EnableGhostIcon(GameController.GetItemByGuid(mOriginalSlot.ItemGuid).Icon);
        SyncGhostIconWithSelectedSlot();
    }

    private void FinishMovingItem(InventorySlot toSlot)
    {
        MoveItemToSlot(mOriginalSlot, toSlot);
        mOriginalSlot = null;
        DisableGhostIcon();
    }

    private void AssignItem(InventorySlot slot, ItemData item)
    {
        int index = InventorySlots.IndexOf(slot);

        if (index == 0)
            OnPlayerWeaponChanged(item.GUID, InventoryChangeType.Pickup);
        else if (index == 1)
            OnPlayerShieldChanged(item.GUID, InventoryChangeType.Pickup);

        slot.HoldItem(item);
    }

    private void UnassignItem(InventorySlot slot)
    {
        int index = InventorySlots.IndexOf(slot);

        if (index == 0)
            OnPlayerWeaponChanged("", InventoryChangeType.Drop);
        else if (index == 1)
            OnPlayerShieldChanged("", InventoryChangeType.Drop);

        slot.DropItem();
    }

    private void MoveItemToSlot(InventorySlot from, InventorySlot to)
    {
        string movedItemGuid = from.ItemGuid;
        string presentItemGuid = to.ItemGuid;

        if (string.IsNullOrEmpty(presentItemGuid)) UnassignItem(from);
        else AssignItem(from, GameController.GetItemByGuid(presentItemGuid));

        AssignItem(to, GameController.GetItemByGuid(movedItemGuid));

        SetSelectedSlot(InventorySlots.IndexOf(to));
    }

    private void EnableGhostIcon(Sprite icon)
    {
        mGhostIcon.style.backgroundImage = icon.texture;
        mGhostIcon.style.visibility = Visibility.Visible;
    }
    
    private void DisableGhostIcon()
    {
        mGhostIcon.style.visibility = Visibility.Hidden;
    }

    private void SyncGhostIconWithPosition(Vector2 position)
    {
        mGhostIcon.style.top = position.y - mGhostIcon.layout.height / 2;
        mGhostIcon.style.left = position.x - mGhostIcon.layout.width / 2;
    }

    private void SyncGhostIconWithSelectedSlot()
    {
        SyncGhostIconWithPosition(
            GetSlotElementByIndex(mSelectedSlotManager.GetSelectedSlotIndex()).worldBound.position);
    }

    private void FadeIn(VisualElement element, int duration)
    {
        element.experimental.animation.Start(new StyleValues { opacity = 0f }, new StyleValues { opacity = 1f }, duration);
    }

    private void FadeOut(VisualElement element, int duration)
    {
        element.experimental.animation.Start(new StyleValues { opacity = 1f }, new StyleValues { opacity = 0f }, duration);
    }

    private void StopAnimatingSlot(int index)
    {
        GetSlotElementByIndex(index).style.backgroundImage = mSlotDefaultBG;
        mSelectedSlotAnimation.ResetAnimation();
    }
}
