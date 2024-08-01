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


    Animator mAnimator;
    float mHeightStanding = 0; // WARNING! Should be const. Defined at Start().
    float mSpeed = 0;
    bool mIsCrouched = false;


    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
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

        if (!IsAttacking())
        {
            // Move player
            Vector3 pos = transform.position;
            pos.x += horizontal * mSpeed * Time.deltaTime;
            pos.z += vertical * mSpeed * Time.deltaTime;
            transform.position = pos;
        }


        // Rotate player
        if (horizontal != 0 || vertical != 0)
        {
            Vector3 rot = transform.eulerAngles;
            rot.y = Mathf.Atan2(horizontal, vertical) * Mathf.Rad2Deg;
            transform.eulerAngles = rot;
        }

        // Set movement animation
        int movementState = 0;
        if (horizontal != 0 || vertical != 0)
        {
            if (mIsCrouched || Mathf.Abs(horizontal) + Mathf.Abs(vertical) < WalkToRunThreshold)
                movementState = 1;
            else
                movementState = 2;
        }
        mAnimator.SetInteger("MovementState", movementState);
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
