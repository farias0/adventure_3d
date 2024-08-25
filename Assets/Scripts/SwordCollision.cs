using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class SwordCollision : MonoBehaviour
{
    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnTriggerEnter(Collider collider)
    {
        if (collider.CompareTag("Enemy"))
        {
            Enemy enemy = collider.GetComponent<Enemy>();
            
            if (enemy.GetHit(Player.Instance.AttackDamage))
            {
                DegradeOnHit();
            }
        }
    }

    private void DegradeOnHit()
    {
        // Assumes it's the weapon equipped by the player,
        // because it doesn't have a proper mechanism to see its ItemDetail


        ItemEntity weapon = InventoryController.Instance.GetEquippedWeapon();

        if (!weapon.Type.Degrades) return;

        weapon.Durability -= 1;

        InventoryController.Instance.RefreshEquippedWeaponDurability();
    }
}
