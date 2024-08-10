using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class RoadSign : MonoBehaviour
{
    public Player Player;
    public float EnableCanvasRadius;

    private Canvas mCanvas;


    // Start is called before the first frame update
    void Start()
    {
        mCanvas = GetComponentInChildren<Canvas>();
        mCanvas.enabled = false;        
    }

    // Update is called once per frame
    void Update()
    {
        if (Vector3.Distance(Player.transform.position, transform.position) < EnableCanvasRadius)
        {
            mCanvas.enabled = true;
        }
        else
        {
            mCanvas.enabled = false;
        }

    }
}
