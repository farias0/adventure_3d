using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueBox : MonoBehaviour
{
    public Dialogue Dialogue;

    private Quaternion mOriginalRotation;
    

    private Canvas mCanvas;
    private TextMeshProUGUI mText;
    private int mCurrentLineIndex = 0;


    public bool IsOn()
    {
        return mCanvas.enabled;
    }

    public void TurnOff()
    {
        mCanvas.enabled = false;
        mCurrentLineIndex = 0;
    }

    public void ShowNext()
    {
        mCanvas.enabled = true;

        if (mCurrentLineIndex < Dialogue.lines.Count)
        {
            mText.text = Dialogue.lines[mCurrentLineIndex].text;
            mCurrentLineIndex++;
        }
        else
        {
            TurnOff();
        }
    }

    // Start is called before the first frame update
    void Start()
    {
        mCanvas = GetComponentInChildren<Canvas>();
        mText = mCanvas.GetComponentInChildren<TextMeshProUGUI>();

        mOriginalRotation = transform.rotation;
    }

    // Update is called once per frame
    void Update()
    {
        transform.rotation = mOriginalRotation; // Don't rotate with its parent
    }
}
