using UnityEngine;

[CreateAssetMenu(menuName = "Game/Tutorial Config", fileName = "TutorialConfig")]
public class TutorialConfig : ScriptableObject
{
    [Header("Hand Prefab")]
    [Tooltip("Drag the TutorialHand prefab here.")]
    public GameObject handPrefab;

    [Header("King Bubble")]
    [Tooltip("The king character sprite for the tutorial speech bubble.")]
    public Sprite kingSprite;
}
