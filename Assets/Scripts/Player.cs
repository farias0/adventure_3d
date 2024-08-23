using System;
using System.Collections;
using System.Collections.Generic;
using System.ComponentModel;
using UnityEngine;
using UnityEngine.AI;
using UnityEngine.UIElements;

// TODO this enum is global. Where should I put it?
enum AnimationMoveState
{
    Idle = 0,
    Walk = 1,
    Run = 2,
    Dead = 3,
    Defend = 4
}

public class Player : MonoBehaviour
{
    public Checkpoint CurrentCheckpoint;
    public ItemData StartingWeapon;
    public ItemData StartingShield;
    public float SpeedStanding;
    public float SpeedCrouched;
    public float IdleToWalkThreshold;
    public float WalkToRunThreshold;
    public float RotationSpeed;
    public float FallSpeed;
    public float InvincibleTime;
    public float InteractionRadius;
    public int AttackDamage = 20;
    public float AttackStaminaCost = 30;
    public float ParryAtackStaminaCost = 18;
    public float StaminaRecoveryRate = 30; // Stamina per second
    public float StaminaRecoveryCooldown = 1.4f; // In seconds
    public float ParryAttackWindowLength = 1.0f;
    public float HearingDistanceStandingMax = 8;
    public float DefendStaminaCost = 12.0f;

    public static Player Instance;

    private const int MaxHealth = 30;
    private const float MaxStamina = 45;
    private const float RespawnTime = 4;
    private const float HeightCrouched = 0.70f;
    private const float DefendColliderOffset = 0.10f; // How far the player's collider offsets to its back when defending.
                                                        // A strategy to remedy problems with the enemy-shield collision

    Animator mAnimator;
    CharacterController mController;
    private CapsuleCollider mCollider;
    private PlayerAudioController mAudioController;
    float mHeightStanding = 0; // WARNING! Should be const. Defined at Start().
    float mSpeed = 0;
    bool mIsCrouched = false;
    float mInvincibleCountdown = 0; // Controls if the player is invincible
    bool mInteractedThisFrame = false;
    float mCharacterRadiusDefault;
    float mCharacterRadiusCrouched; // So the player doesn't float when its squished
    int mHealth;
    float mStamina;
    private float mRespawnCountdown = -1;
    private bool mIsHoldingDefend = false;
    private float mParryAttackWindowCountdown = -1; // Allows attacking after blocking a hit
    private Vector3 mColliderCenterDefault;
    private float mStaminaRecoveryCooldownCountdown = -1;


    public void GetHit(int damage)
    {
        if (mInvincibleCountdown > 0 || mHealth <= 0) return;

        SetHealth(mHealth - damage);

        if (IsDead())
        {
            Die();
            return;
        }

        mInvincibleCountdown = InvincibleTime;

        mAnimator.SetTrigger("GetHit");
    }

    public void HitShield()
    {
        if (mInvincibleCountdown > 0 || mHealth <= 0) return;

        ConsumeStamina(DefendStaminaCost);

        mParryAttackWindowCountdown = ParryAttackWindowLength;

        mAnimator.SetTrigger("DefendGetHit");
        mAudioController.PlaySoundParry();
    }

    public bool InteractedWithMeThisFrame(Vector3 position)
    {
        return mInteractedThisFrame && Vector3.Distance(position, transform.position) < InteractionRadius;
    }

    public bool IsDead()
    {
        return mHealth <= 0;
    }

    public bool IsHoldingDefend()
    {
        return mIsHoldingDefend;
    }


    /// <summary>
    /// Emits a Raycast from the player to the target object.
    /// </summary>
    /// <param name="target"></param>
    /// <returns>If the Raycast hit the target uninterrupted</returns>
    public bool Raycast(GameObject target)
    {
        /*
            ATTENTION: It's possible that at some point the shield starts
            blocking raycasts, removing this function's usefulness for estabilishing
            lines of sight. If this happens, consider throwing two raycasts,
            one of them from the shield.        
        */

        Vector3 direction = target.transform.position - transform.position;
        if (Physics.Raycast(transform.position, direction, out RaycastHit hit))
        {
            return hit.collider.gameObject == target;
        }

        return false;
    }

    /// <summary>
    /// If a creature in the given position can hear the player
    /// this frame.
    /// </summary>
    public bool CanHearPlayer(Vector3 position)
    {
        if (mIsCrouched) return false;

        float hearingDistance;
        Vector3 playerMovement = mController.velocity;
        playerMovement.z = 0;
        hearingDistance = (playerMovement.magnitude * HearingDistanceStandingMax) / SpeedStanding;

        return Vector3.Distance(position, transform.position) < hearingDistance;
    }

    private void Awake()
    {
        if (Instance == null) Instance = this;
        else Destroy(gameObject);
    }

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mController = GetComponent<CharacterController>();
        mCollider = GetComponent<CapsuleCollider>();
        mAudioController = GetComponent<PlayerAudioController>();

        mHeightStanding = transform.localScale.y;
        mSpeed = SpeedStanding;
        
        mCharacterRadiusDefault = mController.radius;
        mCharacterRadiusCrouched = mCharacterRadiusDefault * (HeightCrouched / mHeightStanding);

        mColliderCenterDefault = mController.center;

        InventoryController.OnPlayerWeaponChanged += InventoryController_OnPlayerWeaponChanged;
        InventoryController.OnPlayerShieldChanged += InventoryController_OnPlayerShieldChanged;

        HUDController.Instance.PlayerSetMaxHealth(MaxHealth);
        HUDController.Instance.PlayerSetMaxStamina(MaxStamina);

        if (!InventoryController.Instance.EquipWeapon(StartingWeapon.GUID))
        {
            Debug.LogError("Failed to equip weapon on player start.");
        }
        if (!InventoryController.Instance.EquipShield(StartingShield.GUID))
        {
            Debug.LogError("Failed to equip shield on player start.");
        }

        SetHealth(MaxHealth);
        SetStamina(MaxStamina);
    }

    // Update is called once per frame
    void Update()
    {
        if (mRespawnCountdown > 0)
        {
            mRespawnCountdown -= Time.deltaTime;
            if (mRespawnCountdown <= 0)
            {
                Respawn();
                mRespawnCountdown = -1;
                return; // Without this, movement input overrides the teleport to the spawn point
            }
        }

        if (mParryAttackWindowCountdown > 0) mParryAttackWindowCountdown -= Time.deltaTime;        

        // Apply gravity
        if (!mController.isGrounded)
        {
            Vector3 fall = new(0, FallSpeed * -1, 0);
            mController.Move(fall * Time.deltaTime);
        }


        // Blink if hit recently
        if (mInvincibleCountdown > 0) {
            mInvincibleCountdown -= Time.deltaTime;
            if (mInvincibleCountdown <= 0) SetVisibility(true);
            else BlinkThisFrame();
        }


        if (IsDead()) return;


        // Stamina
        if (mStaminaRecoveryCooldownCountdown > 0)
            mStaminaRecoveryCooldownCountdown -= Time.deltaTime;

        if (mStaminaRecoveryCooldownCountdown <= 0 &&
            !mIsHoldingDefend && !IsAnimationAttack())
        {
            SetStamina(mStamina + StaminaRecoveryRate * Time.deltaTime);
        }


        ProcessInput();

        // Activates weapon during attack
        if (IsAnimationAttack()) SetWeaponActive(true);
        else SetWeaponActive(false);

        // Activates shield during defend
        if (mIsHoldingDefend)
        {
            SetShieldActive(true);
            mCollider.center = mColliderCenterDefault - new Vector3(0, 0, 1) * DefendColliderOffset;
        }
        else
        {
            SetShieldActive(false);
            mCollider.center = mColliderCenterDefault;
        }
    }

    private void InventoryController_OnPlayerWeaponChanged(string itemGuid, InventoryChangeType change)
    {
        Transform container = GetWeaponContainer().transform;

        if (change == InventoryChangeType.Drop && container.childCount == 0)
        {
            Debug.LogError("Inventory asked player to drop weapon, but player has no weapon to drop.");
            return;
        }

        if (container.childCount > 0)
        {
            GameObject weapon = container.GetChild(0).gameObject;
            Destroy(weapon);
        }

        if (change == InventoryChangeType.Pickup)
        {

            ItemData newWeapon = GameController.GetItemByGuid(itemGuid);
            Instantiate(newWeapon.Prefab, container);
        }
    }

    private void InventoryController_OnPlayerShieldChanged(string itemGuid, InventoryChangeType change)
    {
        Transform container = GetShieldContainer().transform;

        if (change == InventoryChangeType.Drop && container.childCount == 0)
        {
            Debug.LogError("Inventory asked player to drop shield, but player has no shield to drop.");
            return;
        }

        if (container.childCount > 0)
        {
            GameObject shield = container.GetChild(0).gameObject;
            Destroy(shield);
        }

        if (change == InventoryChangeType.Pickup)
        {
            ItemData newShield = GameController.GetItemByGuid(itemGuid);
            Instantiate(newShield.Prefab, container);
        }
    }

    private void  ProcessInput()
    {

        Vector3 move = Vector3.zero;


        if (!InventoryController.Instance.IsOpen()) {

            mIsHoldingDefend = Input.GetButton("Defend") && mStamina >= DefendStaminaCost;
            if (Input.GetButtonDown("Attack1")) Attack1();
            if (Input.GetButtonDown("Attack2")) Attack2();
            if (Input.GetButtonDown("Crouch")) CrouchToggle();
            mInteractedThisFrame = Input.GetButtonDown("Interact");
            
            
            float moveX = Input.GetAxis("Horizontal");
            float moveZ = Input.GetAxis("Vertical");

            // For keyboards
            if ((Math.Abs(moveX) == 1) && (Math.Abs(moveZ) == 1))
            {
                moveX *= 0.7071f;
                moveZ *= 0.7071f;
            }

            move = new(moveX, 0, moveZ);
        }


        MovePlayer(move);
    }

    private void SetHealth(int health)
    {
        mHealth = health;
        HUDController.Instance.PlayerSetHealth(health);
    }

    private void SetStamina(float stamina)
    {
        mStamina = Math.Max(stamina, 0);
        mStamina = Math.Min(mStamina, MaxStamina);
        HUDController.Instance.PlayerSetStamina(stamina);
    }

    private void ConsumeStamina(float amount)
    {
        SetStamina(mStamina - amount);
        mStaminaRecoveryCooldownCountdown = StaminaRecoveryCooldown;
    }

    private void Die()
    {
        SetHealth(0);
        mAnimator.SetInteger("MovementState", AnimationMoveState.Dead.GetHashCode());
        mAnimator.SetTrigger("Die");
        mRespawnCountdown = RespawnTime;
    }

    private void Respawn()
    {
        Vector3 spawnPoint = CurrentCheckpoint.GetSpawnPoint();
        transform.position = new(spawnPoint.x, transform.position.y, spawnPoint.z);
        SetHealth(MaxHealth);
        mAnimator.SetInteger("MovementState", AnimationMoveState.Idle.GetHashCode());
        mAnimator.SetTrigger("Respawn");
        GameCamera.Instance.ResetCamera();
        GameController.ResetWorld();
    }

    // ATTENTION: Must be run every frame so the animation updates correctly
    private void MovePlayer(Vector3 move)
    {
        if (move.magnitude > IdleToWalkThreshold)
        {

            if (!IsAnimationAttack() && !IsAnimationGetHIt() && !mIsHoldingDefend)
            {
                // Move player
                mController.Move(mSpeed * Time.deltaTime * move);
            }


            // Rotate player
            Quaternion targetRotation = Quaternion.LookRotation(move);
            transform.rotation = Quaternion.Slerp(transform.rotation, targetRotation, RotationSpeed * Time.deltaTime);
        }

        SetMovementAnimation(move);
    }

    private void SetMovementAnimation(Vector3 move)
    {
        AnimationMoveState animationMoveState = AnimationMoveState.Idle;

        // Not firing the defend animation during the parry attack window
        // allows for counter attacks (the animation state doesn't have an Exit Time)
        if (mParryAttackWindowCountdown <= 0)
        {
            if (mIsHoldingDefend)
            {
                animationMoveState = AnimationMoveState.Defend;
            }
            else if (move.magnitude > IdleToWalkThreshold)
            {
                if (mIsCrouched || Mathf.Abs(move.x) + Mathf.Abs(move.z) < WalkToRunThreshold)
                    animationMoveState = AnimationMoveState.Walk;
                else
                    animationMoveState = AnimationMoveState.Run;
            }
        }

        mAnimator.SetInteger("MovementState", animationMoveState.GetHashCode());
    }

    void CrouchToggle()
    {
        if (mIsHoldingDefend) return;

        mIsCrouched = !mIsCrouched;
        mSpeed = mIsCrouched ? SpeedCrouched : SpeedStanding;

        Vector3 scale = transform.localScale;
        scale.y = mIsCrouched ? HeightCrouched : mHeightStanding;
        transform.localScale = scale;

        mController.radius = mIsCrouched ? mCharacterRadiusCrouched : mCharacterRadiusDefault;
    }

    void Attack1()
    {
        bool isInParryWindow = mParryAttackWindowCountdown > 0;

        if (mIsHoldingDefend && !isInParryWindow) return;
        if (mStamina < AttackStaminaCost && !isInParryWindow) return;
        if (GetEquippedWeaponDetails().Durability <= 0) return; // TODO rethink broken weapon behavior

        mAnimator.SetTrigger("Attack1");

        ConsumeStamina(isInParryWindow ? ParryAtackStaminaCost : AttackStaminaCost);

        if (mParryAttackWindowCountdown > 0) mAudioController.PlaySoundParryAttack();
        mAudioController.PlaySoundAttack1();
    }

    void Attack2()
    {
        bool isInParryWindow = mParryAttackWindowCountdown > 0;

        if (mIsHoldingDefend && !isInParryWindow) return;
        if (mStamina < AttackStaminaCost && !isInParryWindow) return;
        if (GetEquippedWeaponDetails().Durability <= 0) return;

        mAnimator.SetTrigger("Attack2");

        ConsumeStamina(isInParryWindow ? ParryAtackStaminaCost : AttackStaminaCost);

        if (mParryAttackWindowCountdown > 0) mAudioController.PlaySoundParryAttack();
        mAudioController.PlaySoundAttack2();
    }

    void Attack3()
    {
        if (mIsHoldingDefend && mParryAttackWindowCountdown <= 0) return;
        mAnimator.SetTrigger("Attack3");
    }

    bool IsAnimationAttack()
    {
        return mAnimator.GetCurrentAnimatorStateInfo(0).IsTag("Attack");
    }

    bool IsAnimationGetHIt()
    {
        return mAnimator.GetCurrentAnimatorClipInfo(0)[0].clip.name == "GetHit";
    }

    void BlinkThisFrame()
    {
        SetVisibility(Time.timeSinceLevelLoad % 0.1 < 0.05);
    }

    void SetVisibility(bool visible)
    {
        Renderer rend = transform.Find("polySurface1").gameObject.GetComponent<Renderer>();
        rend.enabled = visible;
    }

    private ItemData GetEquippedWeaponDetails()
    {
        return InventoryController.Instance.GetEquippedWeapon();
    }

    private ItemData GetEquippedShieldDetails()
    {
        return InventoryController.Instance.GetEquippedShield();
    }

    private GameObject GetWeaponContainer()
    {
        return transform.Find("root/pelvis/Weapon").gameObject;
    }

    private GameObject GetShieldContainer()
    {
        return transform.Find("root/pelvis/Shield").gameObject;
    }


    void SetWeaponActive(bool isActive)
    {
        Transform container = GetWeaponContainer().transform;
        
        if (container.childCount == 0) return;

        GameObject weapon = container.GetChild(0).gameObject;

        if (!weapon.activeSelf) return;
        
        if (!weapon.TryGetComponent<Collider>(out var collider)) return;

        collider.enabled = isActive;
    }

    void SetShieldActive(bool isActive)
    {
        Transform container = GetShieldContainer().transform;
        
        if (container.childCount == 0) return;

        GameObject shield = container.GetChild(0).gameObject;

        if (!shield.activeSelf) return;
        
        if (!shield.TryGetComponent<Collider>(out var collider)) return;

        collider.enabled = isActive;
    }
}
