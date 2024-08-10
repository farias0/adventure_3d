using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class NpcSimple : MonoBehaviour
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
        mGoToSleepCountdown = -1;
    }

    // Update is called once per frame
    void Update()
    {
        if (Player.InteractedWithMeThisFrame(transform.position))
        {
            if (IsAwake())
            {
                mDialogueBox.ShowNext();
            }
            else
            {
                WakeUp();
            }

            mGoToSleepCountdown = TimeBeforingFallingAsleep;
        }

        else if (IsAwake())
        {
            mGoToSleepCountdown -= Time.deltaTime;

            if (Vector3.Distance(Player.transform.position, transform.position) > TerminateConversationDistance &&
                mDialogueBox.IsOn())
            {
                mDialogueBox.TurnOff();
            }

            if (mGoToSleepCountdown <= 0)
            {
                Sleep();
                mGoToSleepCountdown = -1;
            }
        }
    }

    private bool IsAwake()
    {
        return mGoToSleepCountdown != -1;
    }

    private void Sleep()
    {
        mDialogueBox.TurnOff();
        mAnimator.SetTrigger("Sleep");
    }

    private void WakeUp()
    {
        mAnimator.SetTrigger("WakeUp");
    }
}
