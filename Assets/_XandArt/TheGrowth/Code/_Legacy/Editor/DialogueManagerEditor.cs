using UnityEditor;
using UnityEngine;

[CustomEditor(typeof(DialogueManager))]
public class DialogueManagerEditor : Editor
{
    public override void OnInspectorGUI()
    {
        DrawDefaultInspector();

        DialogueManager dialogueManager = (DialogueManager)target;
        if (GUILayout.Button("Fetch CSV Data"))
        {
            dialogueManager.StartCoroutine(dialogueManager.DownloadCSV());
        }
    }
}