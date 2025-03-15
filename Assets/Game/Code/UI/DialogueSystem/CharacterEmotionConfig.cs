using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(fileName = "CharacterEmotionConfig", menuName = "[Game]/CharacterEmotionConfig")]
public class CharacterEmotionConfig : ScriptableObject
{
    [System.Serializable]
    public class CharacterEmotion
    {
        public string characterName;
        public List<EmotionSprite> emotions;
    }

    [System.Serializable]
    public class EmotionSprite
    {
        public string emotion;
        public Sprite sprite;
    }

    public List<CharacterEmotion> characterEmotions;
}