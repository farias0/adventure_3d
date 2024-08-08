using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject Player;
    public float MinVisibilityX;
    public float MinVisibilityY;

    //private Camera mCamera;
    private float mDistancePlayersHead; // WARNING! Should be const -- defined at Start().
    private float mDistancePlayersBack; // WARNING! Should be const -- defined at Start().


    // Start is called before the first frame update
    void Start()
    {
        //mCamera = GetComponent<Camera>();

        mDistancePlayersHead = transform.position.y - Player.transform.position.y;
        mDistancePlayersBack = transform.position.z - Player.transform.position.z;
    }

    // Update is called once per frame
    void Update()
    {

        /*
            - Quanto mais move em uma direção, mais a câmera se move na outra, até o limite
        */

        //Vector3 playerVelocity = Player.GetComponent<PlayerMovement>().GetVelocity();

        Vector3 pos = transform.position;
        pos.x = Player.transform.position.x;
        pos.y = Player.transform.position.y + mDistancePlayersHead;
        pos.z = Player.transform.position.z + mDistancePlayersBack;
        transform.position = pos;
    }
}
