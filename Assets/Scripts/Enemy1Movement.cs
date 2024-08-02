using System.Collections;
using System.Collections.Generic;
using UnityEngine;

enum EnemyMode {
    Idle,
    Chasing
}

public class Enemy1Movement : MonoBehaviour
{
    public GameObject Player;
    public float SpeedWalking;
    public float SpeedRunning;
    public float RotationSpeed;
    public float DistanceChaseWalk;
    public float DistanceChaseRun;
    public float DistanceChaseStop;

    private Animator mAnimator;
    private Rigidbody mRigidbody;
    private EnemyMode mMode = EnemyMode.Idle;
    private float mSpeed;

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mRigidbody = GetComponent<Rigidbody>();
    }

    // Update is called once per frame
    void Update()
    {

        AnimationMoveState animationMoveState;
        float distanceFromPlayer = Vector3.Distance(transform.position, Player.transform.position);

        if (distanceFromPlayer < DistanceChaseStop || distanceFromPlayer > DistanceChaseWalk)
        {
            mMode = EnemyMode.Idle;
            mSpeed = 0;
            animationMoveState = AnimationMoveState.Idle;
        }
        else if (distanceFromPlayer < DistanceChaseRun)
        {
            mMode = EnemyMode.Chasing;
            mSpeed = SpeedWalking;
            animationMoveState = AnimationMoveState.Walk;
        }
        else
        {
            mMode = EnemyMode.Chasing;
            mSpeed = SpeedRunning;
            animationMoveState = AnimationMoveState.Run;
        }

        if (mMode != EnemyMode.Idle)
        {
            // Rotate towards player
            Vector3 rotateDirection = new(Player.transform.position.x, 0, Player.transform.position.z);
            Quaternion targetRotation = Quaternion.LookRotation(rotateDirection - transform.position);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);


            // Move towards player
            Vector3 moveDirection = (Player.transform.position - transform.position).normalized;
            mRigidbody.MovePosition(transform.position + mSpeed * Time.deltaTime * moveDirection);
        }


        mAnimator.SetInteger("MovementState", animationMoveState.GetHashCode());
    }
}
