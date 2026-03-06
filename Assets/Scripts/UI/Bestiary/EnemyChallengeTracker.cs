using System.Linq;
using UnityEngine;

public class EnemyChallengeTracker : MonoBehaviour
{
    public static EnemyChallengeTracker Instance { get; private set; }

    [SerializeField] private EnemyChallengeSet[] allChallengeSets;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this) Instance = null;
    }

    /// <summary>
    /// Call this when an enemy dies. Pass the killer's info for challenge type matching.
    /// </summary>
    public void ReportEnemyKill(string enemyId, EnemyChallengeType killType = EnemyChallengeType.Kill)
    {
        if (string.IsNullOrEmpty(enemyId)) return;

        var save = GameData.Instance.Save;

        // Always increment generic Kill challenges
        IncrementMatching(save, enemyId, EnemyChallengeType.Kill);

        // Also increment specific kill type if different from generic
        if (killType != EnemyChallengeType.Kill)
            IncrementMatching(save, enemyId, killType);

        GameData.Instance.SaveNow();
    }

    private void IncrementMatching(PlayerSaveData save, string enemyId, EnemyChallengeType type)
    {
        if (allChallengeSets == null) return;

        var set = allChallengeSets.FirstOrDefault(s => s != null && s.enemy != null && s.enemy.id == enemyId);
        if (set == null || set.challenges == null) return;

        foreach (var challenge in set.challenges)
        {
            if (challenge == null || challenge.challengeType != type) continue;

            var progress = GetOrCreateProgress(save, challenge.challengeId);
            if (!progress.claimed)
                progress.progress++;
        }
    }

    public static void ClaimReward(EnemyChallengeDefinition challenge)
    {
        if (challenge == null) return;

        var save = GameData.Instance.Save;
        var progress = GetOrCreateProgress(save, challenge.challengeId);

        if (progress.claimed || progress.progress < challenge.targetCount)
            return;

        progress.claimed = true;

        if (challenge.goldReward > 0 && PlayerCurrencyWallet.Instance != null)
            PlayerCurrencyWallet.Instance.AddGold(challenge.goldReward);

        if (challenge.gemsReward > 0 && PlayerCurrencyWallet.Instance != null)
            PlayerCurrencyWallet.Instance.AddGems(challenge.gemsReward);

        GameData.Instance.SaveNow();
    }

    private static EnemyChallengeProgressData GetOrCreateProgress(PlayerSaveData save, string challengeId)
    {
        var existing = save.enemyChallengeProgress.FirstOrDefault(c => c.challengeId == challengeId);
        if (existing != null) return existing;

        var newData = new EnemyChallengeProgressData { challengeId = challengeId };
        save.enemyChallengeProgress.Add(newData);
        return newData;
    }
}
