using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class AnimationSelectedItemSlot
{
    readonly List<Sprite> mFrames;
    private float mFrameTimer = 0f;
    private int mCurrentFrame = 0;
    private bool mDirection = true; // true = forwards
    private const float FrameDuration = 4 * (1f / 60f);

    public AnimationSelectedItemSlot(List<Sprite> frames)
    {
        mFrames = frames;
    }

    /// <summary>
    /// Ticks the animation
    /// </summary>
    /// <returns>The sprite for the current frame</returns>
    public Sprite TickAnimation()
    {
        mFrameTimer += Time.deltaTime;

        if (mFrameTimer >= FrameDuration)
        {
            mFrameTimer = 0;

            // Next frame
            if (mDirection) mCurrentFrame++;
            else mCurrentFrame--;
        }

        // Ping pong effect
        if (mCurrentFrame >= mFrames.Count)
        {
            mDirection = false;
            mCurrentFrame = mFrames.Count - 2;
        }
        else if (mCurrentFrame < 0)
        {
            mDirection = true;
            mCurrentFrame = 1;
        }

        return mFrames[mCurrentFrame];
    }

    public void ResetAnimation()
    {
        mFrameTimer = 0f;
        mCurrentFrame = 0;
        mDirection = true;
    }
}
