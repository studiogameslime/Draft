using UnityEngine;

[CreateAssetMenu(menuName = "Game/Tutorial Config", fileName = "TutorialConfig")]
public class TutorialConfig : ScriptableObject
{
    [Header("Hand Prefab")]
    [Tooltip("Drag the TutorialHand prefab here.")]
    public GameObject handPrefab;
}
