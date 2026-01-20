using System.Collections.Generic;
using System.Reflection;
using UnityEngine;
using UnityEngine.UI;

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
    bool TryClaim(int level, RewardLane lane, Button button);
}

/// <summary>
/// Owns claiming logic and persistence (GameData).
/// Attach this to LevelRewardWindow root (or any always-alive UI root).
/// </summary>
public class LevelRewardsProgressController : MonoBehaviour, ILevelRewardsProgress
{
    [Header("Config")]
    [SerializeField] private LevelRewardsDatabase rewardsDatabase;
    [SerializeField] private ChestOpeningUI chestOpeningUI;


    // Cached claimed keys (loaded from GameData)
    private readonly HashSet<string> _claimed = new HashSet<string>();

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

        // Must be unlocked by player level
        if (level > PlayerLevel) return false;

        // Must have a reward configured in DB
        var reward = rewardsDatabase != null ? rewardsDatabase.GetReward(level, lane) : null;
        if (reward == null) return false;

        // Must not already be claimed
        if (IsClaimed(level, lane)) return false;

        return true;
    }

    public bool TryClaim(int level, RewardLane lane, Button button)
    {
        if (rewardsDatabase == null)
        {
            Debug.LogError("[LevelRewardsProgressController] rewardsDatabase is not assigned.");
            return false;
        }

        // Ensure GameData ready (important on Android cloud init)
        if (GameData.Instance == null || !GameData.Instance.IsReady)
        {
            Debug.LogWarning("[LevelRewardsProgressController] GameData not ready yet.");
            return false;
        }

        if (!CanClaim(level, lane))
            return false;

        string key = LevelRewardsDatabase.MakeClaimKey(level, lane);

        var reward = rewardsDatabase.GetReward(level, lane);
        if (reward == null)
            return false;

        // Grant reward (gold/gems/chest...)
        GrantReward(reward, button);

        // Mark claimed in memory + Save
        _claimed.Add(key);
        SaveClaimedToGameData(_claimed);

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
        // Load cached claimed list from GameData (if available)
        if (GameData.Instance != null && GameData.Instance.IsReady)
        {
            LoadClaimedFromGameDataInto(_claimed);
        }
    }

    private void OnEnable()
    {
        // In case this UI loads before GameData becomes ready (cloud init),
        // try to load again when enabled.
        if (GameData.Instance != null && GameData.Instance.IsReady && _claimed.Count == 0)
        {
            LoadClaimedFromGameDataInto(_claimed);
        }
    }

    // -----------------------
    // Reward granting
    // -----------------------
    private void GrantReward(LevelRewardEntry reward, Button button)
    {
        var save = GameData.Instance.Save;
        if (save == null)
        {
            Debug.LogError("[LevelRewardsProgressController] GameData.Save is null");
            return;
        }
        var buttonTransform = button.GetComponent<RectTransform>();
        switch (reward.type)
        {
            case RewardType.Gold:
                Debug.Log($"Grant {reward.amount} Gold");
                PlayerCurrencyWallet.Instance.AddGold(reward.amount, buttonTransform);
                break;

            case RewardType.Gems:
                PlayerCurrencyWallet.Instance.AddGems(reward.amount, buttonTransform);
                break;

            case RewardType.Chest:
                // Spec: "open chest immediately"
                if (reward.chest == null)
                {
                    Debug.LogError($"[LevelRewardsProgressController] Chest reward at level {reward.level} has no ChestDefinition assigned.");
                    return;
                }
                OpenChestImmediate(reward.chest);
                break;

            default:
                Debug.LogError($"[LevelRewardsProgressController] Unsupported RewardType: {reward.type}");
                break;
        }
    }

    private void OpenChestImmediate(ChestDefinition chest)
    {
        // This is a minimal implementation.
        // If you already have a chest-opening system/UI, replace this method with a call to it.

        // Example logic (adjust to your ChestDefinition fields):
        chestOpeningUI.gameObject.SetActive(true);
        chestOpeningUI.Show(chest);

    }

    // -----------------------
    // GameData integration
    // -----------------------
    private int GetPlayerLevelFromGameData()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null)
            return 1;

        return Mathf.Max(1, GameData.Instance.Save.playerLevel);
    }

    private void LoadClaimedFromGameDataInto(HashSet<string> target)
    {
        target.Clear();

        var save = GameData.Instance.Save;
        if (save == null || save.claimedLevelRewards == null)
            return;

        for (int i = 0; i < save.claimedLevelRewards.Count; i++)
        {
            string key = save.claimedLevelRewards[i];
            if (!string.IsNullOrEmpty(key))
                target.Add(key);
        }
    }

    private void SaveClaimedToGameData(HashSet<string> claimed)
    {
        var save = GameData.Instance.Save;
        if (save == null)
            return;

        if (save.claimedLevelRewards == null)
            save.claimedLevelRewards = new List<string>();

        save.claimedLevelRewards.Clear();
        foreach (var key in claimed)
            save.claimedLevelRewards.Add(key);

        GameData.Instance.SaveNow();
    }
}
