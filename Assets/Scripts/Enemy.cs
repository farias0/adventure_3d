using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;


public class Enemy : MonoBehaviour
{
    private enum State
    {
        Patrolling,
        Alert,
        Searching,
        Chasing
    }


    public GameObject Player;
    public int Lives;
    public float InvincibleTime;
    public float VisionConeRadius;
    [Range(0,360)]
    public float VisionConeAngle;
    public float WalkSpeed; // Overrides the NavMeshAgent speed
    public float RunSpeed;
    public Transform[] PatrolPoints;
    public float AttackRange;
    public float ConfirmedSightDistance; // Distance when enemy confirms its seeing the player
    public float AlertPhaseDuration; // How long the enemy stays alert after losing sight of the player
    public float SearchPhaseDuration; // How long the enemy searches for the player after losing sight of them
    public float ChasePhaseDuration; // How long the enemy chases the player after losing sight of them
    public float AlertToSearchingDuration; // How long the enemy stays facing the player before going to searching


    private Animator mAnimator;
    private NavMeshAgent mNavMeshAgent;
    private Rigidbody mRigidbody;
    private Collider mAttackCollider;
    private State mState = State.Patrolling;
    private int mLives;
    private float mBlinkCountdown = 0;
    private int mCurrentPatrolPoint = 0;
    private bool mIsAttacking;
    private Vector3 mPlayerLastSeenPosition;
    private float mCurrentPhaseCountdown = -1;
    private float mAlertToSearchingCountdown = -1;
    private bool mSeesPlayer = false;


    public bool IsDead()
    {
        return mAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Dead");
    }

    public void GetHit()
    {
        if (IsDead()) return;

        if (mBlinkCountdown > 0) return;
        
        mLives--;
        mBlinkCountdown = InvincibleTime;
        RotateTowardsPlayerImmediately();
        
        if (mLives <= 0)
        {
            Die();

            Debug.Log("Enemy died");
            if (mLives < 0) Debug.LogError("Enemy with negative lives! Lives: " + mLives);
        }
        else
        {
            mAnimator.SetTrigger("GetHit");
            Debug.Log("Enemy hit but not dead");
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
        mRigidbody = GetComponent<Rigidbody>();
        mAttackCollider = transform.Find("AttackCollider").GetComponent<Collider>();


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

        if (IsDead()) return;


        mIsAttacking = IsAttacking();
        mSeesPlayer = SeesPlayer();


        // Blink if hit recently
        if (mBlinkCountdown > 0) {
            mBlinkCountdown -= Time.deltaTime;
            if (mBlinkCountdown <= 0) SetVisibility(true);
            else BlinkThisFrame();
            StopInPlace();
            return;
        }

        if (mState == State.Patrolling && mSeesPlayer)
        {
            AIStartAlert();
            mPlayerLastSeenPosition = Player.transform.position;
        }

        switch (mState)
        {
            case State.Patrolling:
                PatrolArea();
                break;
            case State.Alert:
                AIRoutineAlert();
                break;
            case State.Searching:
                AIRoutineSearch();
                break;
            case State.Chasing:
                AIRoutineChase();
                break;
        }


        if (mIsAttacking) SetAttackColliderActive(true);
        else SetAttackColliderActive(false);
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) HitPlayer();
    }

    void Ressurect()
    {
        mLives = Lives;
        mAnimator.SetTrigger("Ressurrect");
        mRigidbody.constraints = RigidbodyConstraints.None;
    }

    // Follows the patrol points in order
    void PatrolArea()
    {
        if (PatrolPoints.Length == 0) return;

        if (mNavMeshAgent.remainingDistance < 0.5f)
            mCurrentPatrolPoint = (mCurrentPatrolPoint + 1) % PatrolPoints.Length;

        if (mIsAttacking) return;


        if (Vector3.Distance(mNavMeshAgent.destination, PatrolPoints[mCurrentPatrolPoint].position) < 0.2f)
        {
            // Already going there
            UpdateRotationWhileWalking();
            return;
        }

        // By setting these every frame, we avoid having to control
        // is we're transitioning into patrolling the area
        AnimationSetWalk();
        mNavMeshAgent.speed = WalkSpeed;
        MoveTowards(PatrolPoints[mCurrentPatrolPoint].position);
    }

    void MoveTowards(Vector3 destination)
    {
        mNavMeshAgent.SetDestination(destination);
        UpdateRotationWhileWalking();
    }

    void UpdateRotationWhileWalking()
    {
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

    private void AnimationSetAlert()
    {
        // Awesome gambiarra, but...
        // TODO get an actual alert animation
        mAnimator.SetTrigger("GetHit");
        mAnimator.SetInteger("MovementState", AnimationMoveState.Idle.GetHashCode());
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

    // Updates the enemy state based on what its seeing
    private bool SeesPlayer()
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

    void AIStartAlert()
    {
        Debug.Log("Alert");

        mState = State.Alert;
        AnimationSetAlert();
        mNavMeshAgent.speed = 0;
        StopInPlace();
        FacePosition(mPlayerLastSeenPosition);
        mCurrentPhaseCountdown = AlertPhaseDuration;
        mAlertToSearchingCountdown = AlertToSearchingDuration;
    }

    void AIRoutineAlert()
    {
        if (mSeesPlayer)
        {
            FacePosition(mPlayerLastSeenPosition);

            if (Vector3.Distance(Player.transform.position, transform.position) < ConfirmedSightDistance)
            {
                // Player is right in front of the enemy
                AIStartChase();
                mAlertToSearchingCountdown = -1;
                return;
            }

            mAlertToSearchingCountdown -= Time.deltaTime;
            mCurrentPhaseCountdown = AlertPhaseDuration;

            if (mAlertToSearchingCountdown <= 0)
            {
                // Player caught the enemy's attention
                AIStartSearch();
                mAlertToSearchingCountdown = -1;
                return;
            }
        }
        else
        {
            mCurrentPhaseCountdown -= Time.deltaTime;
            
            if (mCurrentPhaseCountdown <= 0)
            {
                // Back to patrolling
                mState = State.Patrolling;
                mAlertToSearchingCountdown = -1;
                return;
            }

            mAlertToSearchingCountdown = AlertToSearchingDuration;
        }
    }

    void AIStartSearch()
    {
        Debug.Log("Search");

        mState = State.Searching;
        AnimationSetWalk();
        mNavMeshAgent.speed = WalkSpeed;
        MoveTowards(mPlayerLastSeenPosition);
        mCurrentPhaseCountdown = SearchPhaseDuration;
    }

    void AIRoutineSearch()
    {
        if (mSeesPlayer)
        {


            if (Vector3.Distance(Player.transform.position, transform.position) < ConfirmedSightDistance)
            {
                // Player is right in front of the enemy
                AIStartChase();
                return;
            }

            mCurrentPhaseCountdown = SearchPhaseDuration;

            // Update route
            if (mPlayerLastSeenPosition != mNavMeshAgent.destination) MoveTowards(mPlayerLastSeenPosition);

        }
        else if (Vector3.Distance(mPlayerLastSeenPosition, transform.position) < 0.5f)
        {
            mCurrentPhaseCountdown -= Time.deltaTime;

            if (mCurrentPhaseCountdown <= 0)
            {
                // Back to patrolling
                mState = State.Patrolling;
                return;
            }
        }
    }

    void AIStartChase()
    {
        Debug.Log("Chase");

        mState = State.Chasing;
        AnimationSetRun();
        mNavMeshAgent.speed = RunSpeed;
        mCurrentPhaseCountdown = ChasePhaseDuration;
    }

    void AIRoutineChase()
    {
        if (!mIsAttacking)
        {
            float distanceToPlayer = (Player.transform.position - transform.position).magnitude;
            if (distanceToPlayer < AttackRange)
            {
                StopInPlace();
                Attack();
            }
            else
            {
                MoveTowards(Player.transform.position);
            }
        }

        if (mSeesPlayer)
        {
            mCurrentPhaseCountdown = ChasePhaseDuration;
        }
        else
        {
            mCurrentPhaseCountdown -= Time.deltaTime;

            if (mCurrentPhaseCountdown <= 0)
            {
                // Back to searching
                AIStartSearch();
                return;
            }
        }
    }

    // Necessary because the NavMeshAgent aparently doesn't stop immediately
    void StopInPlace()
    {
        mNavMeshAgent.SetDestination(transform.position);
    }

    void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
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

    void SetAttackColliderActive(bool active)
    {
        if (mAttackCollider)
        {
            mAttackCollider.enabled = active;
        }
    }

    private void Die()
    {
        StopInPlace();
        mRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        mAnimator.SetTrigger("Die");
    }
}
