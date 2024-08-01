using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject player;

    float distancePlayersBack; // WARNING! Should be const. Defined at Start().


    // Start is called before the first frame update
    void Start()
    {
        distancePlayersBack = transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {

        // TODO Copy Link's Awakening camera system

        Vector3 pos = transform.position;
        pos.x = player.transform.position.x;
        pos.z = player.transform.position.z + distancePlayersBack;
        transform.position = pos;
    }
}
