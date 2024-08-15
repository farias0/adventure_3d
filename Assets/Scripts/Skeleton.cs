using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : MonoBehaviour
{
    
    public float AttackRange = 1.15f;
    public int AttackDamage = 10;
    public float AlertPhaseDuration = 3.5f;
    public float ConfirmedSightDistance = 9;
    public float RunSpeed = 2.3f;
    public int MaxHealth = 40;


    private Enemy mEnemyComponent;
    private Collider mAttackCollider;


    // Start is called before the first frame update
    void Start()
    {
        mEnemyComponent = GetComponent<Enemy>();
        mAttackCollider = transform.Find("AttackCollider").GetComponent<Collider>();

        mEnemyComponent.AttackRange = AttackRange;
        mEnemyComponent.AttackDamage = AttackDamage;
        mEnemyComponent.AlertPhaseDuration = AlertPhaseDuration;
        mEnemyComponent.ConfirmedSightDistance = ConfirmedSightDistance;
        mEnemyComponent.RunSpeed = RunSpeed;
        mEnemyComponent.MaxHealth = MaxHealth;
    }

    // Update is called once per frame
    void Update()
    {
        if (mEnemyComponent.IsAttacking()) SetAttackColliderActive(true);
        else SetAttackColliderActive(false);
    }

    private void SetAttackColliderActive(bool active)
    {
        if (mAttackCollider)
        {
            mAttackCollider.enabled = active;
        }
    }
}
