using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Enemy1Movement : MonoBehaviour
{

    Animator mAnimator;

    // Start is called before the first frame update
    void Start()
    {
        mAnimator = GetComponent<Animator>();
    }

    // Update is called once per frame
    void Update()
    {
        AnimationMoveState animationMoveState = AnimationMoveState.Idle;
        mAnimator.SetInteger("MovementState", (int)Time.timeSinceLevelLoad % 3);//animationMoveState.GetHashCode());
    }
}
