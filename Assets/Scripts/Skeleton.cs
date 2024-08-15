using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Skeleton : MonoBehaviour
{
    
    public float AttackRange;
    public int AttackDamage;

    private Enemy mEnemyComponent;
    private Collider mAttackCollider;


    // Start is called before the first frame update
    void Start()
    {
        mEnemyComponent = GetComponent<Enemy>();
        mAttackCollider = transform.Find("AttackCollider").GetComponent<Collider>();

        mEnemyComponent.AttackRange = AttackRange;
        mEnemyComponent.AttackDamage = AttackDamage;
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
