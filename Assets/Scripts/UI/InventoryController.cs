#nullable enable

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
    private InventorySlot mWeaponSlot;
    private InventorySlot mShieldSlot;
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
        mOriginalSlot.DisplayBrokenOverlay(false);

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

    /// <summary>
    /// Tries to equip a weapon from the inventory
    /// </summary>
    /// <param name="itemGuid"></param>
    /// <returns>If it was able to equip it</returns>
    public bool EquipWeapon(string itemGuid)
    {
        // TODO make InventorySlots a dictionary
        InventorySlot itemSlot = InventorySlots.FirstOrDefault(slot => slot.ItemGuid == itemGuid);
        
        if (itemSlot == null) return false;

        return MoveItemToSlot(itemSlot, InventorySlots[0]);
    }

    /// <summary>
    /// Tries to equip a shield from the inventory
    /// </summary>
    /// <param name="itemGuid"></param>
    /// <returns>If it was able to equip it</returns>
    public bool EquipShield(string itemGuid)
    {
        InventorySlot itemSlot = InventorySlots.FirstOrDefault(slot => slot.ItemGuid == itemGuid);
        if (itemSlot == null) return false;

        return MoveItemToSlot(itemSlot, InventorySlots[1]);
    }

    public void UpdateDateTime(GameTime time)
    {
        mRoot.Q<Label>("DateTime").text = $"{time.Hour}:{time.Minute:D2}, dia {time.Day}";
    }

    public void RefreshEquippedWeaponDurability()
    {
        int durability = GetEquippedWeapon().GetDurability();
        HUDController.Instance.PlayerSetWeaponDurability(durability);
        
        if (durability <= 0) {
            mWeaponSlot.DisplayBrokenOverlay(true);
            InventorySlot? toSlot = GetEmptySlot();
            if (toSlot == null) Debug.LogError("No empty slot to move the weapon to"); // TODO separate equipment from inventory slots
            else if (!MoveItemToSlot(mWeaponSlot, toSlot))
                Debug.LogError("Couldn't move broken weapon to an empty slot");
        }
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
        
        mWeaponSlot = new();
        InventorySlots.Add(mWeaponSlot);
        mEquipmentSlotContainer.Add(mWeaponSlot);

        mShieldSlot = new();
        InventorySlots.Add(mShieldSlot);
        mEquipmentSlotContainer.Add(mShieldSlot);

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
            if (HUDController.Instance) HUDController.Instance.SetActive(false);
        }
        else
        {
            mSelectedSlotAnimation?.ResetAnimation();
            if (HUDController.Instance) HUDController.Instance.SetActive(true);
        }

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
        fromSlot.DisplayBrokenOverlay(false);
        EnableGhostIcon(GameController.GetItemByGuid(mOriginalSlot.ItemGuid).Icon);
        SyncGhostIconWithSelectedSlot();
    }

    private void FinishMovingItem(InventorySlot toSlot)
    {
        if (MoveItemToSlot(mOriginalSlot, toSlot))
        {
            mOriginalSlot = null;
            DisableGhostIcon();
        }
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

    /// <returns>If was able to move</returns>
    private bool MoveItemToSlot(InventorySlot from, InventorySlot to)
    {
        string movedItemGuid = from.ItemGuid;
        string presentItemGuid = to.ItemGuid;

        ItemData movedItem = GameController.GetItemByGuid(movedItemGuid);
        ItemData presentItem = GameController.GetItemByGuid(presentItemGuid);


        if (from == mWeaponSlot && presentItem && presentItem.GetDurability() <= 0) return false;
        if (to == mWeaponSlot && movedItem.GetDurability() <= 0) return false;


        if (string.IsNullOrEmpty(presentItemGuid)) UnassignItem(from);
        else AssignItem(from, GameController.GetItemByGuid(presentItemGuid));

        AssignItem(to, GameController.GetItemByGuid(movedItemGuid));

        SetSelectedSlot(InventorySlots.IndexOf(to));


        if (from == mWeaponSlot)
        {
            HUDController.Instance.PlayerSetMaxWeaponDurability(presentItem ? presentItem.MaxDurability : 0);
            HUDController.Instance.PlayerSetWeaponDurability(presentItem ? presentItem.GetDurability() : 0);
        }
        else if (to == mWeaponSlot)
        {
            HUDController.Instance.PlayerSetMaxWeaponDurability(movedItem.MaxDurability);
            HUDController.Instance.PlayerSetWeaponDurability(movedItem.GetDurability());
        }

        to.DisplayBrokenOverlay(movedItem.GetDurability() <= 0);
        from.DisplayBrokenOverlay(presentItem && presentItem.GetDurability() <= 0);

        return true;
    }

    private InventorySlot? GetEmptySlot()
    {
        return InventorySlots.FirstOrDefault(x => x.ItemGuid.Equals(""));
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
