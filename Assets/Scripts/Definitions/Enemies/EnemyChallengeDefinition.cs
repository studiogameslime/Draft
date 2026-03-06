using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Challenge Definition")]
public class EnemyChallengeDefinition : ScriptableObject
{
    public string challengeId;
    public string enemyId;
    public EnemyChallengeType challengeType;
    public int targetCount;

    [Header("Rewards")]
    public int goldReward;
    public int gemsReward;
    public int xpReward;

    public string GetDescription()
    {
        string action = challengeType switch
        {
            EnemyChallengeType.Kill => "Kill",
            EnemyChallengeType.KillWithPoison => "Poison",
            EnemyChallengeType.KillWithBurn => "Burn",
            EnemyChallengeType.KillWithCrit => "Crit kill",
            EnemyChallengeType.KillWithMelee => "Melee kill",
            EnemyChallengeType.KillWithRanged => "Ranged kill",
            EnemyChallengeType.KillWithMage => "Mage kill",
            _ => "Defeat"
        };
        return $"{action} {targetCount}";
    }
}

public enum EnemyChallengeType
{
    Kill,
    KillWithPoison,
    KillWithBurn,
    KillWithCrit,
    KillWithMelee,
    KillWithRanged,
    KillWithMage
}
