using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class DialogueUI : MonoBehaviour
{
    public TextMeshProUGUI characterNameText;
    public TextMeshProUGUI dialogueText;
    public Image characterEmotionImage; // Assuming you have images for emotions
    public DialogueManager dialogueManager;
    public CharacterEmotionConfig characterEmotionConfig;

    private int currentDialogueIndex = 0;

    void Start()
    {
        ShowDialogue();
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            OnNextButtonClicked();
        }
    }

    public void ShowDialogue()
    {
        if (dialogueManager.dialogueDataList == null || dialogueManager.dialogueDataList.Count == 0)
            return;

        DialogueData dialogueData = dialogueManager.dialogueDataList[currentDialogueIndex];
        characterNameText.text = dialogueData.Name;
        dialogueText.text = dialogueData.Dialogue;
        SetCharacterEmotionImage(dialogueData.Name, dialogueData.Emotion);

        currentDialogueIndex = (currentDialogueIndex + 1) % dialogueManager.dialogueDataList.Count;
    }

    public void OnNextButtonClicked()
    {
        ShowDialogue();
    }

    private void SetCharacterEmotionImage(string characterName, string emotion)
    {
        foreach (var characterEmotion in characterEmotionConfig.characterEmotions)
        {
            if (characterEmotion.characterName == characterName)
            {
                foreach (var emotionSprite in characterEmotion.emotions)
                {
                    if (emotionSprite.emotion == emotion)
                    {
                        characterEmotionImage.sprite = emotionSprite.sprite;
                        return;
                    }
                }
            }
        }
        // If no matching emotion is found, you can set a default sprite or handle it as needed
        characterEmotionImage.sprite = null;
    }
}