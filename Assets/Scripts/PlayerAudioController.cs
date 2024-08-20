using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerAudioController : MonoBehaviour
{
    public AudioClip Attack1;
    public AudioClip Attack2;

    private AudioSource mAudioSource;

    private void Start()
    {
        mAudioSource = GetComponent<AudioSource>();
    }

    public void PlaySoundAttack1()
    {
        mAudioSource.PlayOneShot(Attack1);
    }

    public void PlaySoundAttack2()
    {
        mAudioSource.PlayOneShot(Attack2);
    }
}
