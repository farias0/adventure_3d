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
    public float VisionConeRadius;
    [Range(0,360)]
    public float VisionConeAngle;

    private Animator mAnimator;
    private Collider mCollider;
    private UnityEngine.AI.NavMeshAgent mNavMeshAgent;
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
        RotateTowardsPlayerImmediately();
        
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
        mCollider = GetComponent<Collider>();
        mNavMeshAgent = GetComponent<UnityEngine.AI.NavMeshAgent>();

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
            StopInPlace();
            return;
        }


        if (IsDead()) return;


        AnimationMoveState animationMoveState = (AnimationMoveState) mAnimator.GetInteger("MovementState");


        if (SeesPlayer())
        {
            MoveTowardsPlayer();
            animationMoveState = AnimationMoveState.Walk;
        }
        else
        {
            StopInPlace();    
            animationMoveState = AnimationMoveState.Idle;
        }


        mAnimator.SetInteger("MovementState", animationMoveState.GetHashCode());
    }

    void OnTriggerStay(Collider other)
    {
        if (IsDead()) return;

        if (other.CompareTag("Player")) HitPlayer();
    }

    void Ressurect()
    {
        mLives = Lives;
        mAnimator.SetTrigger("Ressurrect");
    }

    void MoveTowardsPlayer()
    {
        mNavMeshAgent.SetDestination(Player.transform.position);

        // Rotate while walks
        Vector3 direction = mNavMeshAgent.velocity.normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * mNavMeshAgent.angularSpeed);
        }
    }

    void RotateTowardsPlayerImmediately()
    {
        Vector3 direction = Player.transform.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
    }

    void HitPlayer()
    {
        Player.GetComponent<PlayerMovement>().GetHit();
    }

    bool SeesPlayer()
    {
        Vector3 direction = Player.transform.position - transform.position;
        
        float distance = direction.magnitude;
        if (distance > VisionConeRadius) return false;

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > VisionConeAngle) return false;


        RaycastHit hit;
        if (Physics.Raycast(transform.position, direction, out hit))
            if (hit.collider.gameObject != Player) return false;


        return true;
    }

    // Necessary because the NavMeshAgent aparently doesn't stop immediately
    void StopInPlace()
    {
        mNavMeshAgent.SetDestination(transform.position);
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
