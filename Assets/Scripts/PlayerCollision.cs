using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerCollision : MonoBehaviour
{
    public float pushForce = 2.0f;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }

    void OnControllerColliderHit(ControllerColliderHit hit)
    {
        // https://docs.unity3d.com/ScriptReference/MonoBehaviour.OnControllerColliderHit.html

        Rigidbody body = hit.collider.attachedRigidbody;


        if (body == null || body.isKinematic) return;

        /*
            Currently, the player's vertical movement is managed by PlayerGravity.
            Applying gravity to the player and checking if it's on the floor
                seems needlesly complex for now.
        */
        if (hit.gameObject.CompareTag("Floor")) return;
        

        // Dont push objects below 
        if (hit.moveDirection.y < -0.3f) return;


        // Only push objects to the sides, never up and down
        Vector3 pushDir = new (hit.moveDirection.x, 0, hit.moveDirection.z);


        body.velocity = pushDir * pushForce; // TODO consider player's speed
    }
}
