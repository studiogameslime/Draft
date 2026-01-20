using System.Collections.Generic;
using UnityEngine;

/// <summary>
/// What the UI needs in order to display and claim rewards.
/// </summary>
public interface ILevelRewardsProgress
{
    int PlayerLevel { get; }
    bool IsClaimed(int level, RewardLane lane);
    bool CanClaim(int level, RewardLane lane);

    /// <summary>
    /// Called by UI. Must handle validation + grant reward + persist in GameData.
    /// </summary>
    bool TryClaim(int level, RewardLane lane);
}

/// <summary>
/// Owns claiming logic and persistence (GameData).
/// Attach this to LevelRewardWindow root (or any always-alive UI root).
/// </summary>
public class LevelRewardsProgressController : MonoBehaviour, ILevelRewardsProgress
{
    [Header("Config")]
    [SerializeField] private LevelRewardsDatabase rewardsDatabase;

    // Cached claimed keys (loaded from GameData)
    private HashSet<string> _claimed = new HashSet<string>();

    // -----------------------
    // ILevelRewardsProgress
    // -----------------------
    public int PlayerLevel => GetPlayerLevelFromGameData();

    public bool IsClaimed(int level, RewardLane lane)
    {
        string key = LevelRewardsDatabase.MakeClaimKey(level, lane);
        return _claimed.Contains(key);
    }

    public bool CanClaim(int level, RewardLane lane)
    {
        if (level <= 0) return false;
        if (level > PlayerLevel) return false;

        var reward = rewardsDatabase != null ? rewardsDatabase.GetReward(level, lane) : null;
        if (reward == null) return false;

        return true;
    }

    public bool TryClaim(int level, RewardLane lane)
    {
        if (rewardsDatabase == null)
        {
            Debug.LogError("[LevelRewardsProgressController] rewardsDatabase is not assigned.");
            return false;
        }

        if (!CanClaim(level, lane))
            return false;

        string key = LevelRewardsDatabase.MakeClaimKey(level, lane);
        if (_claimed.Contains(key))
            return false;

        var reward = rewardsDatabase.GetReward(level, lane);
        if (reward == null)
            return false;

        // Grant reward
        GrantReward(reward);

        // Mark claimed + save to GameData
        _claimed.Add(key);
        SaveClaimedToGameData(_claimed);

        // Optional: notify UI listeners (window will refresh anyway)
        OnClaimed?.Invoke(level, lane);

        return true;
    }

    // -----------------------
    // Events
    // -----------------------
    public System.Action<int, RewardLane> OnClaimed;

    // -----------------------
    // Unity
    // -----------------------
    private void Awake()
    {
        LoadClaimedFromGameDataInto(_claimed);
    }

    // -----------------------
    // Reward granting hooks
    // -----------------------
    private void GrantReward(LevelRewardEntry reward)
    {
        switch (reward.type)
        {
            case RewardType.Gold:
                // TODO: Replace with your wallet system.
                // Example: PlayerCurrencyWallet.Instance.AddGold(reward.amount);
                Debug.Log($"[Rewards] Grant GOLD x{reward.amount}");
                break;

            case RewardType.Chest:
                // Spec: "open chest immediately" after claim.
                // TODO: Replace with your chest flow.
                // Example: ChestOpener.Instance.OpenChest(reward.amount or chestId)
                Debug.Log($"[Rewards] Grant CHEST x{reward.amount} (open immediately)");
                break;
        }
    }

    // -----------------------
    // GameData integration (YOU MUST CONNECT THIS)
    // -----------------------
    private int GetPlayerLevelFromGameData()
    {
        // TODO: Replace with your GameData.
        // Example: return GameData.Instance.PlayerLevel;
        return 1;
    }

    private void LoadClaimedFromGameDataInto(HashSet<string> target)
    {
        target.Clear();

        // TODO: Replace with your GameData.
        // Example:
        // foreach (var key in GameData.Instance.ClaimedLevelRewardKeys)
        //     target.Add(key);
    }

    private void SaveClaimedToGameData(HashSet<string> claimed)
    {
        // TODO: Replace with your GameData.
        // Example:
        // GameData.Instance.ClaimedLevelRewardKeys = claimed.ToList();
        // GameData.Instance.Save();
    }
}
