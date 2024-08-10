using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcSimple : MonoBehaviour
{
    public PlayerMovement Player;
    public float TimeBeforingFallingAsleep;

    Animator mAnimator;
    float mGoToSleepCountdown;

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();

        Sleep();
        mGoToSleepCountdown = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (mGoToSleepCountdown != -1)
        {
            mGoToSleepCountdown -= Time.deltaTime;
            if (mGoToSleepCountdown <= 0)
            {
                Sleep();
                mGoToSleepCountdown = -1;
            }
        }
        else // Is sleeping already
        {
            if (Player.InteractedWithMeThisFrame(transform.position))
            {
                WakeUp();
                mGoToSleepCountdown = TimeBeforingFallingAsleep;
            }
        }
    }

    void Sleep()
    {
        mAnimator.SetTrigger("Sleep");
    }

    void WakeUp()
    {
        mAnimator.SetTrigger("WakeUp");
    }
}
