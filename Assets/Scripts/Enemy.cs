using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Enemy : MonoBehaviour
{
    public GameObject Player;
    public int Lives;
    public float InvincibleTime;
    public float VisionConeRadius;
    [Range(0,360)]
    public float VisionConeAngle;
    public float WalkSpeed; // Overrides the NavMeshAgent speed
    public float RunSpeed;
    public Transform[] PatrolPoints;

    private Animator mAnimator;
    private NavMeshAgent mNavMeshAgent;
    private int mLives;
    private float mBlinkCountdown = 0;
    private int mCurrentPatrolPoint = 0;


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

    public void HitPlayer()
    {
        
        if (IsDead()) return;

        Player.GetComponent<PlayerMovement>().GetHit();
    }


    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
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


        // Otherwise the first patrol point would be the second one in the list
        mCurrentPatrolPoint = PatrolPoints.Length - 1;

        // We don't want the player pushing the enemy around
        Physics.IgnoreCollision(GetComponent<Collider>(), Player.GetComponent<Collider>());

        // Disabling auto-braking allows for continuous movement
        // between points (ie, the agent doesn't slow down as it
        // approaches a destination point).
        mNavMeshAgent.autoBraking = false;
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.M)) Ressurect(); // FOR DEBUGGING
        if (Input.GetKeyDown(KeyCode.N)) Attack(); // FOR DEBUGGING


        // Blink if hit recently
        if (mBlinkCountdown > 0) {
            mBlinkCountdown -= Time.deltaTime;
            if (mBlinkCountdown <= 0) SetVisibility(true);
            else BlinkThisFrame();
            StopInPlace();
            return;
        }


        if (IsDead()) return;


        if (SeesPlayer()) ChasePlayer();
        else PatrolArea();
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) HitPlayer();
    }

    void Ressurect()
    {
        mLives = Lives;
        mAnimator.SetTrigger("Ressurrect");
    }

    void ChasePlayer()
    {
        if (!IsAttacking())
        {
            AnimationSetRun();
            mNavMeshAgent.speed = RunSpeed;
            MoveTowards(Player.transform.position);
        }
    }

    // Follows the patrol points in order
    void PatrolArea()
    {
        if (PatrolPoints.Length == 0) return;

        if (mNavMeshAgent.remainingDistance < 0.5f)
            mCurrentPatrolPoint = (mCurrentPatrolPoint + 1) % PatrolPoints.Length;

        if (!IsAttacking())
        {
            // By setting these every frame, we avoid having to control
            // is we're transitioning into patrolling the area
            AnimationSetWalk();
            mNavMeshAgent.speed = WalkSpeed;
            MoveTowards(PatrolPoints[mCurrentPatrolPoint].position);
        }
    }

    void MoveTowards(Vector3 destination)
    {
        mNavMeshAgent.SetDestination(destination);

        // Rotate while walks
        Vector3 direction = mNavMeshAgent.velocity.normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * mNavMeshAgent.angularSpeed);
        }
    }

    private void Attack()
    {
        mAnimator.SetTrigger("Attack");
    }

    private void AnimationSetIdle()
    {
        mAnimator.SetInteger("MovementState", AnimationMoveState.Idle.GetHashCode());
    }

    private void AnimationSetWalk()
    {
        mAnimator.SetInteger("MovementState", AnimationMoveState.Walk.GetHashCode());
    }

    private void AnimationSetRun()
    {
        mAnimator.SetInteger("MovementState", AnimationMoveState.Run.GetHashCode());
    }

    bool IsAttacking()
    {
        return mAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    void RotateTowardsPlayerImmediately()
    {
        Vector3 direction = Player.transform.position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
    }

    bool SeesPlayer()
    {
        Vector3 direction = Player.transform.position - transform.position;
        
        float distance = direction.magnitude;
        if (distance > VisionConeRadius) return false;

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > VisionConeAngle) return false;


        bool raycastFront =
            Physics.Raycast(transform.position, direction, out RaycastHit hit) &&
            (hit.collider.gameObject != Player);


        // If we don't fire a raycast to the player's feet,
        // if they're crouched we might miss them
        Vector3 directionFeet = Player.transform.position -
            new Vector3(0, (float)(Player.transform.localScale.y + 0.05), 0) - transform.position;
        bool raycastFeet =
            Physics.Raycast(transform.position, directionFeet, out RaycastHit hitFeet) &&
            (hitFeet.collider.gameObject != Player);


        if (!raycastFront && !raycastFeet) return false;


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
