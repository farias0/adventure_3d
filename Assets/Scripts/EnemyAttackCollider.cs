using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class EnemyAttackCollider : MonoBehaviour
{
    private Enemy mEnemy;

    // Start is called before the first frame update
    void Start()
    {
        mEnemy = GetComponentInParent<Enemy>();
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("Player")) mEnemy.HitPlayer();
        Debug.Log("EnemyAttackCollider::OnTriggerStay");
    }
}
