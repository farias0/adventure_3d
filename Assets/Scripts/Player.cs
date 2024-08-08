using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

// TODO this enum is global. Where should I put it?
enum AnimationMoveState
{
    Idle = 0,
    Walk = 1,
    Run = 2
}

public class PlayerMovement : MonoBehaviour
{
    // TODO consider making some of these constants to clear up the component in the inspector
    public GameObject Weapon;
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
    }

    // Update is called once per frame
    void Update()
    {
        //if (Input.GetKeyDown(KeyCode.M)) Debug.Log("Is crouched: " + mIsCrouched.ToString()); // FOR DEBUGGING


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


        Vector3 move = new(moveX, 0, moveZ);
        AnimationMoveState animationMoveState = AnimationMoveState.Idle;


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


            // Set movement animation
            if (mIsCrouched || Mathf.Abs(moveX) + Mathf.Abs(moveZ) < WalkToRunThreshold)
                animationMoveState = AnimationMoveState.Walk;
            else
                animationMoveState = AnimationMoveState.Run;
        }


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
        // TODO maybe don't run this every frame
        if (IsAttacking()) SetWeaponActive(true);
        else SetWeaponActive(false);


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

    void SetWeaponActive(bool active)
    {
        Collider collider = Weapon.GetComponent<Collider>();
        collider.enabled = active;
    }
}
