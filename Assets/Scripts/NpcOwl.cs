using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcOwl : MonoBehaviour
{
    public Player Player;
    public float TimeBeforingFallingAsleep;
    public float TerminateConversationDistance;

    Animator mAnimator;
    DialogueBox mDialogueBox;
    float mGoToSleepCountdown;

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
        mDialogueBox = GetComponentInChildren<DialogueBox>();

        Sleep();
    }

    // Update is called once per frame
    void Update()
    {
        if (Player.InteractedWithMeThisFrame(transform.position))
        {
            if (IsAwake())
            {
                mDialogueBox.ShowNext();
                if (TimeManager.IsDay()) mGoToSleepCountdown = TimeBeforingFallingAsleep;
            }
            else
            {
                WakeUp();
            }
        }

        if (TimeManager.IsDay() && mGoToSleepCountdown <= 0 && mGoToSleepCountdown != -1 && !mDialogueBox.IsOn())
        {
            Sleep();
        }
        else if (IsAwake() && TimeManager.IsDay())
        {
            mGoToSleepCountdown -= Time.deltaTime;
        }
        else if (!IsAwake() && !TimeManager.IsDay())
        {
            WakeUp();
        }

        if (mDialogueBox.IsOn() &&
            Vector3.Distance(Player.transform.position, transform.position) > TerminateConversationDistance)
        {
            mDialogueBox.TurnOff();
        }
    }

    private bool IsAwake()
    {
        return mGoToSleepCountdown > 0;
    }

    private void Sleep()
    {
        mDialogueBox.TurnOff();
        mAnimator.SetTrigger("Sleep");
        mGoToSleepCountdown = -1;
    }

    private void WakeUp()
    {
        mAnimator.SetTrigger("WakeUp");
        mGoToSleepCountdown = TimeBeforingFallingAsleep;
    }
}
