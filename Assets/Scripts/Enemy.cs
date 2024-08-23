using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.Rendering;


public class Enemy : MonoBehaviour
{
    private enum State
    {
        Patrol,
        Alert,
        Search,
        Chase
    }


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
    public int AttackDamage = 10;
    public int MaxHealth = 40;

    private const float EnemyHearPlayerDuration = 0.3f; // For how long the enemy has to hear the player for it to register

    private Animator mAnimator;
    private NavMeshAgent mNavMeshAgent;
    private Rigidbody mRigidbody;
    private State mState;
    private Player mPlayer;
    private int mHealth;
    private float mInvincibleCountdown = 0;
    private int mCurrentPatrolPoint = 0;
    private bool mIsAttacking;
    private Vector3 mPlayerKnownPosition;
    private float mCurrentPhaseCountdown = -1;
    private float mAlertToSearchingCountdown = -1;
    private bool mAwareOfPlayer = false;
    private bool mIsTouchingPlayerShield = false; // Helps avoiding having the enemy hit through the player's shield
    private float mEnemyHearPlayerCountdown = -1; // Controls for how long the enemy has to hear the player for it to register
    private bool mHeardPlayerLastFrame = false; // To be used with HearsPlayer()


    public bool IsAnimationDead()
    {
        return mAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Dead");
    }

    public bool IsAnimationAttack()
    {
        return mAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    public void GetHit(int damage)
    {
        if (IsAnimationDead()) return;

        if (mInvincibleCountdown > 0) return;
        
        mHealth -= damage;
        mInvincibleCountdown = InvincibleTime;
        FacePosition(mPlayer.transform.position);

        if (damage > 0) Debug.Log("Enemy hit! Health: " + mHealth);
        else Debug.Log("Enemy hit shield");
        
        if (mHealth <= 0)
        {
            Die();
        }
        else
        {
            mAnimator.SetTrigger("GetHit");
        }
    }

    public void HitPlayer(int damage)
    {
        if (IsAnimationDead()) return;

        if (mIsTouchingPlayerShield)
        {
            HitShield();
            return;
        }

        mPlayer.GetHit(damage);
    }


    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mNavMeshAgent = GetComponent<NavMeshAgent>();
        mRigidbody = GetComponent<Rigidbody>();
        mPlayer = Player.Instance;


        if (MaxHealth <= 0)
        {
            Debug.LogError("Enemy with invalid max health");
            mHealth = 1;
        }
        else
        {
            mHealth = MaxHealth;
        }

        // We don't want the player pushing the enemy around
        Physics.IgnoreCollision(GetComponent<Collider>(), mPlayer.GetComponent<Collider>());

        // Disabling auto-braking allows for continuous movement
        // between points (ie, the agent doesn't slow down as it
        // approaches a destination point).
        mNavMeshAgent.autoBraking = false;

        GameController.AddWorldResetListener(Respawn);

        AIStartPatrol();
    }

    // Update is called once per frame
    void Update()
    {
        if (IsAnimationDead()) return;


        mIsAttacking = IsAnimationAttack();
        
        mAwareOfPlayer = SeesPlayer() || HearsPlayer();
        if (mAwareOfPlayer) mPlayerKnownPosition = mPlayer.transform.position;


        /*
            OnTriggerExit() isn't called when the player's shield is disabled,
            so we have to check for this manually.
        */
        if (mIsTouchingPlayerShield && !mPlayer.IsHoldingDefend())
            mIsTouchingPlayerShield = false;


        // Enemy hit recently
        if (mInvincibleCountdown > 0) {
            mInvincibleCountdown -= Time.deltaTime;
            if (mInvincibleCountdown <= 0) SetModelVisibility(true);
            else
            {
                StopInPlace();
                BlinkThisFrame();
                return;
            }
        }


        switch (mState)
        {
            case State.Patrol:
                AIRoutinePatrol();
                break;
            case State.Alert:
                AIRoutineAlert();
                break;
            case State.Search:
                AIRoutineSearch();
                break;
            case State.Chase:
                AIRoutineChase();
                break;
        }
    }

    void OnTriggerEnter(Collider other)
    {
        if (mInvincibleCountdown > 0) return;

        if (other.CompareTag("Player")) HitPlayer(AttackDamage);
        if (other.CompareTag("Shield"))
        {
            mIsTouchingPlayerShield = true;
            if (IsAnimationAttack()) HitShield();
        }

    }

    void OnTriggerExit(Collider other)
    {
        if (other.CompareTag("Shield")) mIsTouchingPlayerShield = false;
    }

    private void Attack()
    {
        mAnimator.SetTrigger("Attack");
    }

    public void HitShield()
    {
        mPlayer.HitShield();
        GetHit(0); // TODO use another animation for getting parried, and replace the "!IsGettingHit()" check in OnTriggerStay
    }

    private void Die()
    {
        SetModelVisibility(true);
        StopInPlace();
        mRigidbody.constraints = RigidbodyConstraints.FreezeAll;
        mAnimator.SetTrigger("Die");
    }

    private void Respawn()
    {
        mHealth = MaxHealth;
        mAnimator.SetTrigger("Ressurrect");
        mRigidbody.constraints = RigidbodyConstraints.None;
        AIStartPatrol();
    }

    private void MoveTowards(Vector3 destination, float speed)
    {
        mNavMeshAgent.speed = speed;
        mNavMeshAgent.SetDestination(destination);
        UpdateRotationWhileWalking();

        if (speed == 0) AnimationSetIdle();
        else if (speed == WalkSpeed) AnimationSetWalk();
        else if (speed == RunSpeed) AnimationSetRun();
    }

    private void StopInPlace()
    {
        AnimationSetIdle();
        mNavMeshAgent.SetDestination(transform.position);
        mNavMeshAgent.speed = 0;
    }

    private void FacePosition(Vector3 position)
    {
        Vector3 direction = position - transform.position;
        direction.y = 0;
        Quaternion lookRotation = Quaternion.LookRotation(direction);
        transform.rotation = lookRotation;
    }

    private void UpdateRotationWhileWalking()
    {
        Vector3 direction = mNavMeshAgent.velocity.normalized;
        if (direction != Vector3.zero)
        {
            Quaternion lookRotation = Quaternion.LookRotation(direction);
            transform.rotation = Quaternion.Slerp(transform.rotation, lookRotation, Time.deltaTime * mNavMeshAgent.angularSpeed);
        }
    }

    private void BlinkThisFrame()
    {
        SetModelVisibility(Time.timeSinceLevelLoad % 0.1 < 0.05);
    }

    private void SetModelVisibility(bool visible)
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

    private bool SeesPlayer()
    {
        if (mPlayer.IsDead()) return false;


        Vector3 direction = mPlayer.transform.position - transform.position;
        

        float distance = direction.magnitude;
        if (distance > VisionConeRadius) return false;

        float angle = Vector3.Angle(transform.forward, direction);
        if (angle > VisionConeAngle) return false;


        if (!mPlayer.Raycast(gameObject)) return false;


        return true;
    }

    // ATTENTION: To be called every frame
    private bool HearsPlayer()
    {
        bool hearingConfirmed = false;
        bool hearsSomething = mPlayer.CanHearPlayer(transform.position);

        if (!hearsSomething) mEnemyHearPlayerCountdown = -1;
        else if (!mHeardPlayerLastFrame) mEnemyHearPlayerCountdown = EnemyHearPlayerDuration; // Starts counting
        else {
            mEnemyHearPlayerCountdown -= Time.deltaTime;
            if (mEnemyHearPlayerCountdown <= 0) hearingConfirmed = true;
        }

        mHeardPlayerLastFrame = hearsSomething;

        return hearingConfirmed;
    }

    private void AIStartPatrol()
    {
        mState = State.Patrol;
        MoveTowards(PatrolPoints[mCurrentPatrolPoint].position, WalkSpeed);
    }

    private void AIRoutinePatrol()
    {
        if (mAwareOfPlayer)
        {   
            // Alert
            AIStartAlert();
            return;
        }

        if (PatrolPoints.Length == 0) return;

        if (mNavMeshAgent.remainingDistance < 0.5f)
        {
            // Next point
            mCurrentPatrolPoint = (mCurrentPatrolPoint + 1) % PatrolPoints.Length;
            MoveTowards(PatrolPoints[mCurrentPatrolPoint].position, WalkSpeed);
        }

        UpdateRotationWhileWalking();
    }

    private void AIStartAlert()
    {
        mState = State.Alert;
        AnimationSetAlert();
        StopInPlace();
        FacePosition(mPlayerKnownPosition);
        mCurrentPhaseCountdown = AlertPhaseDuration;
        mAlertToSearchingCountdown = AlertToSearchingDuration;
    }

    private void AIRoutineAlert()
    {
        if (mPlayer.IsDead())
        {
            AIStartPatrol();
            return;
        }

        if (mAwareOfPlayer)
        {
            FacePosition(mPlayerKnownPosition);

            if (Vector3.Distance(mPlayer.transform.position, transform.position) < ConfirmedSightDistance)
            {
                // Saw player clearly
                AIStartChase();
                mAlertToSearchingCountdown = -1;
                return;
            }

            mAlertToSearchingCountdown -= Time.deltaTime;

            if (mAlertToSearchingCountdown <= 0)
            {
                // Player caught the enemy's attention
                AIStartSearch();
                mAlertToSearchingCountdown = -1;
                return;
            }

            mCurrentPhaseCountdown = AlertPhaseDuration;
        }
        else
        {
            mCurrentPhaseCountdown -= Time.deltaTime;
            
            if (mCurrentPhaseCountdown <= 0)
            {
                // Back to patrolling
                AIStartPatrol();
                mAlertToSearchingCountdown = -1;
                return;
            }

            mAlertToSearchingCountdown = AlertToSearchingDuration;
        }
    }

    private void AIStartSearch()
    {
        mState = State.Search;
        MoveTowards(mPlayerKnownPosition, WalkSpeed);
        mCurrentPhaseCountdown = SearchPhaseDuration;
    }

    private void AIRoutineSearch()
    {
        if (mPlayer.IsDead())
        {
            AIStartPatrol();
            return;
        }

        UpdateRotationWhileWalking();

        if (mAwareOfPlayer)
        {
            if (Vector3.Distance(mPlayer.transform.position, transform.position) < ConfirmedSightDistance)
            {
                // Found player
                AIStartChase();
                return;
            }

            mCurrentPhaseCountdown = SearchPhaseDuration;

            // Update route
            if (mPlayerKnownPosition != mNavMeshAgent.destination) MoveTowards(mPlayerKnownPosition, WalkSpeed);

        }
        else if (Vector3.Distance(mPlayerKnownPosition, transform.position) < 0.5f)
        {
            
            mCurrentPhaseCountdown -= Time.deltaTime;
            StopInPlace();

            if (mCurrentPhaseCountdown <= 0)
            {
                // Back to patrolling
                AIStartPatrol();
                return;
            }
        }
    }

    private void AIStartChase()
    {
        mState = State.Chase;
        mCurrentPhaseCountdown = ChasePhaseDuration;
    }

    private void AIRoutineChase()
    {
        if (mPlayer.IsDead())
        {
            AIStartPatrol();
            return;
        }

        if (!mIsAttacking)
        {
            float distanceToPlayer = (mPlayer.transform.position - transform.position).magnitude;
            if (distanceToPlayer < AttackRange)
            {
                // Attack player
                StopInPlace();
                Attack();
            }
            else
            {
                // Chase player
                MoveTowards(mPlayer.transform.position, RunSpeed);
            }
        }

        if (mAwareOfPlayer)
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
}
