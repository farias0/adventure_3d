using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy1Movement : MonoBehaviour
{
    public GameObject Player;
    public float Speed;
    public float RotationSpeed;

    private Animator mAnimator;
    private Rigidbody mRigidbody;

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        // Rotate towards player
        Vector3 rotateDirection = new(Player.transform.position.x, 0, Player.transform.position.z);
        Quaternion targetRotation = Quaternion.LookRotation(rotateDirection - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);


        // Move towards player
        Vector3 moveDirection = (Player.transform.position - transform.position).normalized;
        mRigidbody.MovePosition(transform.position + Speed * Time.deltaTime * moveDirection);


        // Set movement animation
        AnimationMoveState animationMoveState = AnimationMoveState.Idle;
        mAnimator.SetInteger("MovementState", (int)Time.timeSinceLevelLoad % 3);//animationMoveState.GetHashCode());
    }
}
