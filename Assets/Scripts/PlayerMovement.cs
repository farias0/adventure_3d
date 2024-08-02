using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerMovement : MonoBehaviour
{
    public float SpeedWalking;
    public float SpeedCrouched;
    public float HeightCrouched;
    public float WalkToRunThreshold;
    public float RotationSpeed;

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
        mSpeed = SpeedWalking;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) Debug.Log("Is crouched: " + mIsCrouched.ToString()); // FOR DEBUGGING


        if (Input.GetButtonDown("Fire1")) Attack();
        if (Input.GetButtonDown("Fire2")) CrouchToggle();

        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");



        // For keyboards
        if ((horizontal == 1 || horizontal == -1) && (vertical == 1 || vertical == -1))
        {
            horizontal *= 0.7071f;
            vertical *= 0.7071f;
        }


        Vector3 move = new(horizontal, 0, vertical);
        int animationMoveState = 0;


        if (move != Vector3.zero)
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
            if (mIsCrouched || Mathf.Abs(horizontal) + Mathf.Abs(vertical) < WalkToRunThreshold)
                animationMoveState = 1;
            else
                animationMoveState = 2;
        }

       
        mAnimator.SetInteger("MovementState", animationMoveState);
    }

    void CrouchToggle()
    {
        mIsCrouched = !mIsCrouched;
        mSpeed = mIsCrouched ? SpeedCrouched : SpeedWalking;

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
