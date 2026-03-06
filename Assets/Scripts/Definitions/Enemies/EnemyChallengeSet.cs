using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Challenge Set")]
public class EnemyChallengeSet : ScriptableObject
{
    public UnitDefinition enemy;
    public EnemyChallengeDefinition[] challenges;
}
