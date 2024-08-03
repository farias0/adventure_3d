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
    public int Lives;
    public float InvincibleTime;

    private Animator mAnimator;
    private Rigidbody mRigidbody;
    private Collider mCollider;
    private int mLives;
    private float mBlinkCountdown = 0;


    public bool IsDead()
    {
        return mLives == 0;
    }

    public void GetHit()
    {
        if (mLives < 0)
        {
            Debug.LogError("Enemy with negative lives! Lives: " + mLives);
            return;
        }

        if (IsDead()) return;

        if (mBlinkCountdown > 0) return;
        
        mLives--;
        mBlinkCountdown = InvincibleTime;
        
        if (IsDead())
        {
            mAnimator.SetTrigger("Die");
        }
        else
        {
            mAnimator.SetTrigger("GetHit");
        }
    }


    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mRigidbody = GetComponent<Rigidbody>();
        mCollider = GetComponent<Collider>();

        if (Lives < 0)
        {
            Debug.LogError("Enemy with negative lives");
            mLives = 0;
        }
        else
        {
            mLives = Lives;
        }


        // We don't want the player pushing the enemy around
        Physics.IgnoreCollision(GetComponent<Collider>(), Player.GetComponent<Collider>());
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) Ressurect(); // FOR DEBUGGING


        // Blink if hit recently
        if (mBlinkCountdown > 0) {
            mBlinkCountdown -= Time.deltaTime;
            if (mBlinkCountdown <= 0) SetVisibility(true);
            else BlinkThisFrame();
        }


        if (mLives == 0) return; // Dead :(


        AnimationMoveState animationMoveState = (AnimationMoveState) mAnimator.GetInteger("MovementState");
        // float distanceFromPlayer = Vector3.Distance(transform.position, Player.transform.position);

        
        // if (!IsHit()) {
        //     if (distanceFromPlayer > 1)
        //     {
        //         RotateTowardsPlayer();
        //         MoveTowardsPlayer(SpeedWalking);
        //         animationMoveState = AnimationMoveState.Walk;
        //     }
        //     else
        //     {
        //         animationMoveState = AnimationMoveState.Idle;
        //     }
        // }


        // Manually check for collision with the player
        if (mCollider.bounds.Intersects(Player.GetComponent<Collider>().bounds))
        {

            HitPlayer();
        }


        mAnimator.SetInteger("MovementState", animationMoveState.GetHashCode());
    }

    void Ressurect()
    {
        mLives = Lives;
        mAnimator.SetTrigger("Ressurrect");
    }

    void MoveTowardsPlayer(float speed)
    {
        Vector3 playerDirection = new(Player.transform.position.x, 0, Player.transform.position.z);
        Vector3 moveDirection = (playerDirection - transform.position).normalized;
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

    void HitPlayer()
    {
        Player.GetComponent<PlayerMovement>().GetHit();
    }

    void BlinkThisFrame()
    {
        SetVisibility(Time.timeSinceLevelLoad % 0.1 < 0.05);
    }

    void SetVisibility(bool visible)
    {
        for (int i = 0; i < transform.childCount; i++)
        {
            // This checks if the child has a renderer component.
            // The linter suggested this to avoid needlesly
            // allocating the component if it doesn't have one.
            if (!transform.GetChild(i).gameObject.TryGetComponent<Renderer>(out var rend)) continue;

            rend.enabled = visible;
        }
    }
}
