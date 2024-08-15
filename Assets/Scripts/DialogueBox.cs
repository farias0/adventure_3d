using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;

public class DialogueBox : MonoBehaviour
{
    public List<Dialogue> Dialogue;

    private Quaternion mOriginalRotation;
    

    private Canvas mCanvas;
    private TextMeshProUGUI mText;
    private int mCurrentDialogue = 0;
    private int mCurrentLineIndex = 0;


    public void SetDialogue(int index)
    {
        if (index < Dialogue.Count)
        {
            mCurrentDialogue = index;
            mCurrentLineIndex = 0;
        }
    }

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
        if (!mCanvas.enabled) {
            GameCamera.Instance.SmoothlyResetCamera();
        }

        mCanvas.enabled = true;

        if (mCurrentLineIndex < Dialogue[mCurrentDialogue].lines.Count)
        {
            mText.text = Dialogue[mCurrentDialogue].lines[mCurrentLineIndex].text;
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
