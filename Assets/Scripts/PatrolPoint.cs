using UnityEngine;

public class PatrolPoint : MonoBehaviour
{
    /*
        This script allows the the PatrolPoints to be children
        of an Object without moving and rotating with it.
    */

    private Vector3 worldPosition;
    private Quaternion worldRotation;

    void Start()
    {
        // Store the initial world position and rotation
        worldPosition = transform.position;
        worldRotation = transform.rotation;
    }

    void LateUpdate()
    {
        // Restore the world position and rotation
        transform.position = worldPosition;
        transform.rotation = worldRotation;
    }
}