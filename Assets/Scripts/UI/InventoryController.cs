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

    readonly List<Sprite> mAnimFrames;
    readonly VisualElement mSlotContainer;
    private StyleBackground defaultBG;
    private int mSelectedSlotIndex = 0;
    private float mAnimFrameTimer = 0f;
    private int mAnimCurrentFrame = 0;
    private bool mAnimDirection = true; // true = forwards
    private const float FrameDuration = 4 * (1f / 60f);
    private const int SlotsPerRow = 5;

    public SelectedSlotManager(VisualElement slotContainer, List<Sprite> animationFrames)
    {
        mSlotContainer = slotContainer;
        mAnimFrames = animationFrames;

        defaultBG = mSlotContainer[0].style.backgroundImage;
    }

    public void TickAnimation()
    {
        mAnimFrameTimer += Time.deltaTime;

        if (mAnimFrameTimer >= FrameDuration)
        {
            mAnimFrameTimer = 0;

            // Next frame
            if (mAnimDirection) mAnimCurrentFrame++;
            else mAnimCurrentFrame--;
        }

        // Ping pong effect
        if (mAnimCurrentFrame >= mAnimFrames.Count)
        {
            mAnimDirection = false;
            mAnimCurrentFrame = mAnimFrames.Count - 2;
        }
        else if (mAnimCurrentFrame < 0)
        {
            mAnimDirection = true;
            mAnimCurrentFrame = 1;
        }

        mSlotContainer[mSelectedSlotIndex].style.backgroundImage =
            mAnimFrames[mAnimCurrentFrame].texture;
    }

    public void ResetAnimation()
    {
        mAnimFrameTimer = 0f;
        mAnimCurrentFrame = 0;
        mAnimDirection = true;
    }

    public void SetSelectedSlot(int index)
    {
        mSlotContainer[mSelectedSlotIndex].style.backgroundImage = defaultBG;
        mSelectedSlotIndex = index;
    }

    public void MoveCursor(CursorDirection dir)
    {
        mSlotContainer[mSelectedSlotIndex].style.backgroundImage = defaultBG;

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
    public List<InventorySlot> InventoryItems = new();
    public List<Sprite> SelectedSlotAnimFrames;


    private static bool mIsDragging;
    private static InventorySlot mOriginalSlot;
    private static VisualElement mGhostIcon;

    // Allows the static DragAndDrop methods to talk to the
    // single InventoryController instance in the scene.
    public static InventoryController Instance { get; private set; }


    private VisualElement mRoot;
    private VisualElement mSlotContainer;
    private bool mIsInventoryOpen;
    private SelectedSlotManager mSelectedSlotManager;
    private SelectedSlotManager.CursorDirection? mLastCursorDirection = null;


    private const float GamepadDeadzone = 0.25f; 


    public static void StartDrag(Vector2 position, InventorySlot originalSlot)
    {
        //Set tracking variables
        mIsDragging = true;
        mOriginalSlot = originalSlot;

        //Set the new position
        mGhostIcon.style.top = position.y - mGhostIcon.layout.height / 2;
        mGhostIcon.style.left = position.x - mGhostIcon.layout.width / 2;

        //Set the image
        mGhostIcon.style.backgroundImage = GameController.GetItemByGuid(originalSlot.ItemGuid).Icon.texture;

        //Flip the visibility on
        mGhostIcon.style.visibility = Visibility.Visible;

        // Select the slot that is being dragged
        Instance.mSelectedSlotManager.SetSelectedSlot(
            Instance.InventoryItems.IndexOf(originalSlot));
    }

    public bool IsOpen()
    {
        return mIsInventoryOpen;
    }

    // Start is called before the first frame update
    void Start()
    {
        mSelectedSlotManager = new SelectedSlotManager(mSlotContainer, SelectedSlotAnimFrames);
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInput();

        if (!mIsInventoryOpen) return;

        mSelectedSlotManager.TickAnimation();
    }

    private void Awake()
    {
        Instance = this;

        //Store the root from the UI Document component
        mRoot = GetComponent<UIDocument>().rootVisualElement;

        mGhostIcon = mRoot.Query<VisualElement>("GhostIcon");

        //Search the root for the SlotContainer Visual Element
        mSlotContainer = mRoot.Q<VisualElement>("SlotContainer");

        //Create InventorySlots and add them as children to the SlotContainer
        for (int i = 0; i < 20; i++)
        {
            InventorySlot item = new();

            InventoryItems.Add(item);

            mSlotContainer.Add(item);
        }

        GameController.OnInventoryChanged += GameController_OnInventoryChanged;

        mGhostIcon.RegisterCallback<PointerMoveEvent>(OnPointerMove);
        mGhostIcon.RegisterCallback<PointerUpEvent>(OnPointerUp);

        //A little gambiarra -- the inventory starts closed!
        mIsInventoryOpen = true;
        ToggleInventory();
    }

    private void ProcessInput()
    {

        if (Input.GetButtonDown("ToggleInventory")) ToggleInventory();

        if (!mIsInventoryOpen) return;


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
            mSelectedSlotManager.MoveCursor(cursorDirection.Value);

        mLastCursorDirection = cursorDirection;
    }

    private void GameController_OnInventoryChanged(string[] itemGuid, InventoryChangeType change)
    {
        //Loop through each item and if it has been picked up, add it to the next empty slot
        foreach (string item in itemGuid)
        {
            if (change == InventoryChangeType.Pickup)
            {
                var emptySlot = InventoryItems.FirstOrDefault(x => x.ItemGuid.Equals(""));
                            
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
            mSelectedSlotManager?.ResetAnimation();

        // Lock or unlock the cursor
        // Cursor.lockState = isInventoryOpen ? CursorLockMode.None : CursorLockMode.Locked;
        // Cursor.visible = isInventoryOpen;

        // Handle pausing/unpausing the game
        // Time.timeScale = isInventoryOpen ? 0f : 1f;
    }

    private void OnPointerMove(PointerMoveEvent evt)
    {
        //Only take action if the player is dragging an item around the screen
        if (!mIsDragging) return;


        //Set the new position
        mGhostIcon.style.top = evt.position.y - mGhostIcon.layout.height / 2;
        mGhostIcon.style.left = evt.position.x - mGhostIcon.layout.width / 2;

    }

    private void OnPointerUp(PointerUpEvent evt)
    {
        if (!mIsDragging) return;


        //Check to see if they are dropping the ghost icon over any inventory slots.
        IEnumerable<InventorySlot> slots = InventoryItems.Where(x => x.worldBound.Overlaps(mGhostIcon.worldBound));

        //Found at least one
        if (slots.Count() != 0)
        {
            InventorySlot closestSlot = slots.OrderBy(x => Vector2.Distance(x.worldBound.position, mGhostIcon.worldBound.position)).First();
            
            //Set the new inventory slot with the data
            closestSlot.HoldItem(GameController.GetItemByGuid(mOriginalSlot.ItemGuid));
            
            //Clear the original slot
            mOriginalSlot.DropItem();

            // Set the selected slot to the new slot
            mSelectedSlotManager.SetSelectedSlot(InventoryItems.IndexOf(closestSlot));
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

    private void FadeIn(VisualElement element, int duration)
    {
        element.experimental.animation.Start(new StyleValues { opacity = 0f }, new StyleValues { opacity = 1f }, duration);
    }

    private void FadeOut(VisualElement element, int duration)
    {
        element.experimental.animation.Start(new StyleValues { opacity = 1f }, new StyleValues { opacity = 0f }, duration);
    }
}
