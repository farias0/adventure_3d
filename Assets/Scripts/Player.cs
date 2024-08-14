using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

// TODO this enum is global. Where should I put it?
enum AnimationMoveState
{
    Idle = 0,
    Walk = 1,
    Run = 2
}

public class Player : MonoBehaviour
{
    public float SpeedStanding;
    public float SpeedCrouched;
    public float HeightCrouched;
    public float IdleToWalkThreshold;
    public float WalkToRunThreshold;
    public float RotationSpeed;
    public float FallSpeed;
    public float InvincibleTime;
    public float InteractionRadius;

    Animator mAnimator;
    CharacterController mController;
    float mHeightStanding = 0; // WARNING! Should be const. Defined at Start().
    float mSpeed = 0;
    bool mIsCrouched = false;
    float mInvincibleCountdown = 0; // Controls if the player is invincible
    bool mInteractedThisFrame = false;
    float mCharacterRadiusDefault;
    float mCharacterRadiusCrouched; // So the player doesn't float when its squished


    public void GetHit()
    {
        if (mInvincibleCountdown > 0) return;

        mAnimator.SetTrigger("GetHit");
        mInvincibleCountdown = InvincibleTime;
    }

    public bool InteractedWithMeThisFrame(Vector3 position)
    {
        return mInteractedThisFrame && Vector3.Distance(position, transform.position) < InteractionRadius;
    }

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mController = GetComponent<CharacterController>();

        mHeightStanding = transform.localScale.y;
        mSpeed = SpeedStanding;
        
        mCharacterRadiusDefault = mController.radius;
        mCharacterRadiusCrouched = mCharacterRadiusDefault * (HeightCrouched / mHeightStanding);

        InventoryController.OnPlayerWeaponChanged += InventoryController_OnPlayerWeaponChanged;
        InventoryController.OnPlayerShieldChanged += InventoryController_OnPlayerShieldChanged;
    }

    // Update is called once per frame
    void Update()
    {

        ProcessInput();


        // Apply gravity
        if (!mController.isGrounded)
        {
            Vector3 fall = new(0, FallSpeed * -1, 0);
            mController.Move(fall * Time.deltaTime);
        }


        // Blink if hit recently
        if (mInvincibleCountdown > 0) {
            mInvincibleCountdown -= Time.deltaTime;
            if (mInvincibleCountdown <= 0) SetVisibility(true);
            else BlinkThisFrame();
        }


        // Activates weapon during attack
        if (IsAttacking()) SetWeaponActive(true);
        else SetWeaponActive(false);
    }

    private void InventoryController_OnPlayerWeaponChanged(string itemGuid, InventoryChangeType change)
    {
        Transform container = GetWeaponContainer().transform;

        if (change == InventoryChangeType.Drop && container.childCount == 0)
        {
            Debug.LogError("Inventory asked player to drop weapon, but player has no weapon to drop.");
            return;
        }

        if (container.childCount > 0)
        {
            GameObject weapon = container.GetChild(0).gameObject;
            Destroy(weapon);
        }

        if (change == InventoryChangeType.Pickup)
        {

            ItemData newWeapon = GameController.GetItemByGuid(itemGuid);
            Instantiate(newWeapon.Prefab, container);
        }
    }

    private void InventoryController_OnPlayerShieldChanged(string itemGuid, InventoryChangeType change)
    {
        Transform container = GetShieldContainer().transform;

        if (change == InventoryChangeType.Drop && container.childCount == 0)
        {
            Debug.LogError("Inventory asked player to drop shield, but player has no shield to drop.");
            return;
        }

        if (container.childCount > 0)
        {
            GameObject shield = container.GetChild(0).gameObject;
            Destroy(shield);
        }

        if (change == InventoryChangeType.Pickup)
        {
            ItemData newShield = GameController.GetItemByGuid(itemGuid);
            Instantiate(newShield.Prefab, container);
        }
    }

    private void  ProcessInput()
    {

        Vector3 move = Vector3.zero;


        if (!InventoryController.Instance.IsOpen()) {

            if (Input.GetButtonDown("Attack1")) Attack1();
            if (Input.GetButtonDown("Attack2")) Attack2();
            //if (Input.GetButtonDown("Attack3")) Attack3();
            if (Input.GetButtonDown("Crouch")) CrouchToggle();
            mInteractedThisFrame = Input.GetButtonDown("Interact");
            
            
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // For keyboards
            if ((Math.Abs(moveX) == 1) && (Math.Abs(moveZ) == 1))
            {
                moveX *= 0.7071f;
                moveZ *= 0.7071f;
            }

            move = new(moveX, 0, moveZ);
        }


        MovePlayer(move);
    }

    // ATTENTION: Must be run every frame so the animation updates correctly
    private void MovePlayer(Vector3 move)
    {
        if (move.magnitude > IdleToWalkThreshold)
        {

            if (!IsAttacking() && !IsGettingHit())
            {
                // Move player
                mController.Move(mSpeed * Time.deltaTime * move);
            }


            // Rotate player
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }

        SetMovementAnimation(move);
    }

    private void SetMovementAnimation(Vector3 move)
    {
        AnimationMoveState animationMoveState = AnimationMoveState.Idle;

        if (move.magnitude > IdleToWalkThreshold)
        {
            if (mIsCrouched || Mathf.Abs(move.x) + Mathf.Abs(move.z) < WalkToRunThreshold)
                animationMoveState = AnimationMoveState.Walk;
            else
                animationMoveState = AnimationMoveState.Run;
        }

        mAnimator.SetInteger("MovementState", animationMoveState.GetHashCode());
    }

    void CrouchToggle()
    {
        mIsCrouched = !mIsCrouched;
        mSpeed = mIsCrouched ? SpeedCrouched : SpeedStanding;

        Vector3 scale = transform.localScale;
        scale.y = mIsCrouched ? HeightCrouched : mHeightStanding;
        transform.localScale = scale;

        mController.radius = mIsCrouched ? mCharacterRadiusCrouched : mCharacterRadiusDefault;
    }

    void Attack1()
    {
        mAnimator.SetTrigger("Attack1");
    }

    void Attack2()
    {
        mAnimator.SetTrigger("Attack2");
    }

    void Attack3()
    {
        mAnimator.SetTrigger("Attack3");
    }

    bool IsAttacking()
    {
        return mAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    bool IsGettingHit()
    {
        return mAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "GetHit";
    }

    void BlinkThisFrame()
    {
        SetVisibility(Time.timeSinceLevelLoad % 0.1 < 0.05);
    }

    void SetVisibility(bool visible)
    {
        Renderer rend = transform.Find("polySurface1").gameObject.GetComponent<Renderer>();
        rend.enabled = visible;
    }

    private ItemData GetEquippedWeaponDetails()
    {
        return InventoryController.Instance.GetEquippedWeapon();
    }

    private ItemData GetEquippedShieldDetails()
    {
        return InventoryController.Instance.GetEquippedShield();
    }

    private GameObject GetWeaponContainer()
    {
        return transform.Find("root/pelvis/Weapon").gameObject;
    }

    private GameObject GetShieldContainer()
    {
        return transform.Find("root/pelvis/Shield").gameObject;
    }


    void SetWeaponActive(bool isActive)
    {
        Transform container = GetWeaponContainer().transform;
        
        if (container.childCount == 0) return;

        GameObject weapon = container.GetChild(0).gameObject;

        if (!weapon.activeSelf) return;
        
        if (!weapon.TryGetComponent<Collider>(out var collider)) return;

        collider.enabled = isActive;
    }
}
