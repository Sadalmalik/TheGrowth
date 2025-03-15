using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Networking;
using GeekyHouse.Architecture.CSV;

public class DialogueManager : MonoBehaviour
{
    public string csvUrl;
    public List<DialogueData> dialogueDataList;

    void Start()
    {
        StartCoroutine(DownloadCSV());
    }

    public IEnumerator DownloadCSV()
    {
        using (UnityWebRequest webRequest = UnityWebRequest.Get(csvUrl))
        {
            yield return webRequest.SendWebRequest();

            if (webRequest.result == UnityWebRequest.Result.ConnectionError || webRequest.result == UnityWebRequest.Result.ProtocolError)
            {
                Debug.LogError(webRequest.error);
            }
            else
            {
                string csvText = webRequest.downloadHandler.text;
                dialogueDataList = ObjectCSVConverter.FromCSV<DialogueData>(csvText);
                // Now you have the dialogue data in dialogueDataList
            }
        }
    }
}