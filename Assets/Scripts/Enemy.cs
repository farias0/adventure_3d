using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum EnemyMode {
    Idle,
    Chasing
}

public class Enemy : MonoBehaviour
{
    public GameObject Player;
    public float SpeedWalking;
    public float SpeedRunning;
    public float RotationSpeed;
    public float DistanceChaseRun;
    public float DistanceChaseWalk;
    public float DistanceChaseStop;

    private Animator mAnimator;
    private Rigidbody mRigidbody;


    public void GetHit()
    {
        mAnimator.SetTrigger("GetHit");
    }


    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {
        AnimationMoveState animationMoveState = (AnimationMoveState) mAnimator.GetInteger("MovementState");
        float distanceFromPlayer = Vector3.Distance(transform.position, Player.transform.position);

        
        if (!IsHit()) {
            if (distanceFromPlayer < DistanceChaseStop)
            {
                RotateTowardsPlayer();
                animationMoveState = AnimationMoveState.Idle;
            }
            else if (distanceFromPlayer < DistanceChaseWalk)
            {
                RotateTowardsPlayer();
                MoveTowardsPlayer(SpeedWalking);
                animationMoveState = AnimationMoveState.Walk;
            }
            else if (distanceFromPlayer < DistanceChaseRun)
            {
                RotateTowardsPlayer();
                MoveTowardsPlayer(SpeedRunning);
                animationMoveState = AnimationMoveState.Run;
            }
            else
            {
                animationMoveState = AnimationMoveState.Idle;
            }
        }


        mAnimator.SetInteger("MovementState", animationMoveState.GetHashCode());
    }

    void MoveTowardsPlayer(float speed)
    {
        // WARNING: It's moving vertically too. It shouldn't.
        Vector3 moveDirection = (Player.transform.position - transform.position).normalized;
        mRigidbody.MovePosition(transform.position + speed * Time.deltaTime * moveDirection);
    }

    void RotateTowardsPlayer()
    {
        Vector3 rotateDirection = new(Player.transform.position.x, 0, Player.transform.position.z);
        Quaternion targetRotation = Quaternion.LookRotation(rotateDirection - transform.position);
        transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
    }

    bool IsHit()
    {
        return mAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "GetHit";
    }
}
