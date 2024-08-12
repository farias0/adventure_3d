using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using UnityEngine.UIElements;
using UnityEngine.UIElements.Experimental;


class SelectedSlotAnimation
{
    readonly List<Sprite> mFrames;
    float mFrameTimer = 0f;
    int mCurrentFrame = 0;
    bool mDirection = true; // true = forwards

    private const float FrameDuration = 4 * (1f / 60f);

    public SelectedSlotAnimation(List<Sprite> frames)
    {
        mFrames = frames;
    }

    public Sprite GetFrameAndTick()
    {
        mFrameTimer += Time.deltaTime;

        if (mFrameTimer >= FrameDuration)
        {
            mFrameTimer = 0;

            // Next frame
            if (mDirection) mCurrentFrame++;
            else mCurrentFrame--;
        }

        // Ping pong effect
        if (mCurrentFrame >= mFrames.Count)
        {
            mDirection = false;
            mCurrentFrame = mFrames.Count - 2;
        }
        else if (mCurrentFrame < 0)
        {
            mDirection = true;
            mCurrentFrame = 1;
        }

        return mFrames[mCurrentFrame];
    }

    public void Reset()
    {
        mFrameTimer = 0f;
        mCurrentFrame = 0;
        mDirection = true;
    }
}

public class InventoryController : MonoBehaviour
{
    public List<InventorySlot> InventoryItems = new();
    public List<Sprite> SelectedSlotAnimFrames;


    private static bool mIsDragging;
    private static InventorySlot mOriginalSlot;
    private static VisualElement mGhostIcon;

    private VisualElement mRoot;
    private VisualElement mSlotContainer;
    private bool mIsInventoryOpen;
    private int mSelectedSlotIndex = 0;
    private SelectedSlotAnimation mSelectedSlotAnimation;


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
    }

    public bool IsOpen()
    {
        return mIsInventoryOpen;
    }

    // Start is called before the first frame update
    void Start()
    {
        mSelectedSlotAnimation = new SelectedSlotAnimation(SelectedSlotAnimFrames);
    }

    // Update is called once per frame
    void Update()
    {
        ProcessInput();

        if (!mIsInventoryOpen) return;

        UpdatedSelectedSlotAnimation();
    }

    private void Awake()
    {
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
            mSelectedSlotAnimation?.Reset();

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

    private void UpdatedSelectedSlotAnimation()
    {
        mSlotContainer[mSelectedSlotIndex].style.backgroundImage =
            mSelectedSlotAnimation.GetFrameAndTick().texture;
    }
}
