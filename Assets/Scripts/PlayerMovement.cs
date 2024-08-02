using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum AnimationMoveState
{
    Idle = 0,
    Walk = 1,
    Run = 2
}

public class PlayerMovement : MonoBehaviour
{
    // TODO consider making some of these constants to clear up the component in the inspector
    public float SpeedStanding;
    public float SpeedCrouched;
    public float HeightCrouched;
    public float IdleToWalkThreshold;
    public float WalkToRunThreshold;
    public float RotationSpeed;
    public float FallSpeed;

    Animator mAnimator;
    CharacterController mController;
    float mHeightStanding = 0; // WARNING! Should be const. Defined at Start().
    float mSpeed = 0;
    bool mIsCrouched = false;


    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mController = GetComponent<CharacterController>();

        mHeightStanding = transform.localScale.y;
        mSpeed = SpeedStanding;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) Debug.Log("Is crouched: " + mIsCrouched.ToString()); // FOR DEBUGGING


        if (Input.GetButtonDown("Fire1")) Attack();
        if (Input.GetButtonDown("Fire2")) CrouchToggle();

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

            if (!IsAttacking())
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

       
        mAnimator.SetInteger("MovementState", animationMoveState.GetHashCode());
    }

    void CrouchToggle()
    {
        mIsCrouched = !mIsCrouched;
        mSpeed = mIsCrouched ? SpeedCrouched : SpeedStanding;

        Vector3 scale = transform.localScale;
        scale.y = mIsCrouched ? HeightCrouched : mHeightStanding;
        transform.localScale = scale;
    }

    void Attack()
    {
        mAnimator.SetTrigger("Attack");
    }

    bool IsAttacking()
    {
        return mAnimator.GetCurrentAnimatorStateInfo(0).IsName("Attack");
    }
}
