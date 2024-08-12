using System;
using System.CodeDom.Compiler;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;


class SelectedSlotManager
{
    public enum CursorDirection
    {
        Left,
        Right,
        Up,
        Down
    }


    readonly VisualElement mSlotContainer;
    private int mSelectedSlotIndex = 0;
    private const int SlotsPerRow = 5;


    public SelectedSlotManager(VisualElement slotContainer)
    {
        mSlotContainer = slotContainer;
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
                mSelectedSlotIndex--;
                if (mSelectedSlotIndex < 0) mSelectedSlotIndex = mSlotContainer.childCount - 1;
                break;
            case CursorDirection.Right:
                mSelectedSlotIndex++;
                if (mSelectedSlotIndex >= mSlotContainer.childCount) mSelectedSlotIndex = 0;
                break;
            case CursorDirection.Up:
                mSelectedSlotIndex -= SlotsPerRow;
                if (mSelectedSlotIndex < 0) mSelectedSlotIndex += mSlotContainer.childCount;
                break;
            case CursorDirection.Down:
                mSelectedSlotIndex += SlotsPerRow;
                if (mSelectedSlotIndex >= mSlotContainer.childCount) mSelectedSlotIndex -= mSlotContainer.childCount;
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


    private VisualElement mRoot;
    private VisualElement mEquipmentContainer;
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


    public void StartDrag(Vector2 position, InventorySlot originalSlot)
    {
        mIsDragging = true;
        mOriginalSlot = originalSlot;

        EnableGhostIcon(GameController.GetItemByGuid(mOriginalSlot.ItemGuid).Icon);
        SyncGhostIconWithPosition(position);

        StopAnimatingSlot(mSelectedSlotManager.GetSelectedSlotIndex());
        mSelectedSlotManager.SetSelectedSlot(InventorySlots.IndexOf(originalSlot));
    }

    public bool IsOpen()
    {
        return mIsInventoryOpen;
    }

    // Start is called before the first frame update
    void Start()
    {
        mSelectedSlotManager = new SelectedSlotManager(mInventorySlotContainer);
        mSelectedSlotAnimation = new AnimationSelectedItemSlot(SelectedSlotAnimFrames);
        mSlotDefaultBG = mInventorySlotContainer[3].style.backgroundImage;
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInput();

        if (!mIsInventoryOpen) return;

        mInventorySlotContainer[mSelectedSlotManager.GetSelectedSlotIndex()].style.backgroundImage
            = mSelectedSlotAnimation.TickAnimation().texture;
    }

    private void Awake()
    {
        Instance = this;

        //Store the root from the UI Document component
        mRoot = GetComponent<UIDocument>().rootVisualElement;

        mGhostIcon = mRoot.Query<VisualElement>("GhostIcon");

        mEquipmentContainer = mRoot.Q<VisualElement>("EquipmentContainer");
        mInventorySlotContainer = mRoot.Q<VisualElement>("SlotContainer");
        
        InventorySlot sword = new();
        //InventorySlots.Add(sword); //TODO
        mInventorySlotContainer.Add(sword);
        mEquipmentContainer.Add(sword);

        InventorySlot shield = new();
        //InventorySlots.Add(shield); //TODO
        mInventorySlotContainer.Add(shield);
        mEquipmentContainer.Add(shield);

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
                var emptySlot = InventorySlots.FirstOrDefault(x => x.ItemGuid.Equals(""));
                            
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
            FadeIn(mRoot, 250);
        else
            mSelectedSlotAnimation?.ResetAnimation();

        // Lock or unlock the cursor
        // Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        // Cursor.visible = isInventoryOpen;

        // Handle pausing/unpausing the game
        // Time.timeScale = isInventoryOpen ? 0f : 1f;
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

    private void MoveItemToSlot(InventorySlot from, InventorySlot to)
    {
        string movedItemGuid = from.ItemGuid;
        string presentItemGuid = to.ItemGuid;

        if (string.IsNullOrEmpty(presentItemGuid)) from.DropItem();
        else from.HoldItem(GameController.GetItemByGuid(presentItemGuid));

        to.HoldItem(GameController.GetItemByGuid(movedItemGuid));

        StopAnimatingSlot(mSelectedSlotManager.GetSelectedSlotIndex());
        mSelectedSlotManager.SetSelectedSlot(InventorySlots.IndexOf(to));
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
            mInventorySlotContainer[mSelectedSlotManager.GetSelectedSlotIndex()].worldBound.position);
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
        mInventorySlotContainer[index].style.backgroundImage = mSlotDefaultBG;
        mSelectedSlotAnimation.ResetAnimation();
    }
}
