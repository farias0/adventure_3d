using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class CameraFollow : MonoBehaviour
{
    public GameObject Player;
    public float MinVisibilityHor;
    public float MinVisibilityVer;
    public float Speed;

    private float mDistancePlayersHead; // WARNING! Should be const -- defined at Start().
    private float mDistancePlayersBack; // WARNING! Should be const -- defined at Start().
    private float mOffsetHor;
    private float mOffsetVer;
    private Vector3 mPlayerPositionLastFrame;


    // Start is called before the first frame update
    void Start()
    {
        mDistancePlayersHead = transform.position.y - Player.transform.position.y;
        mDistancePlayersBack = transform.position.z - Player.transform.position.z;

        mPlayerPositionLastFrame = Player.transform.position;
    }

    // Update is called once per frame
    void Update()
    {
        
        Vector3 playerMovement = Player.transform.position - mPlayerPositionLastFrame;

        if (playerMovement.x > 0) // Player moving to the right
        {
            mOffsetHor = Mathf.Min(mOffsetHor + playerMovement.x * Speed, MinVisibilityHor);
        }
        else if (playerMovement.x < 0) // Player moving to the left
        {
            mOffsetHor = Mathf.Max(mOffsetHor + playerMovement.x * Speed, -MinVisibilityHor);
        }

        Vector3 pos = transform.position;
        pos.x = Player.transform.position.x + mOffsetHor;
        pos.y = Player.transform.position.y + mDistancePlayersHead;
        pos.z = Player.transform.position.z + mDistancePlayersBack + mOffsetVer;
        transform.position = pos;


        mPlayerPositionLastFrame = Player.transform.position;
    }
}
