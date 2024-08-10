using System.Collections;
using System.Collections.Generic;
using UnityEngine;

[System.Serializable]
public class DialogueLine
{
    [TextArea(3, 10)]
    public string text;
}

[CreateAssetMenu(fileName = "New Dialogue", menuName = "Dialogue/Conversation")]
public class Dialogue : ScriptableObject
{
    public List<DialogueLine> lines;

    // Start is called before the first frame update
    void Start()
    {
        
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
