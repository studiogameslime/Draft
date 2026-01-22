using System;
using System.Collections.Generic;
using UnityEngine;



[Serializable]
public class LevelRewardEntry
{
    [Min(1)] public int level = 1;
    public RewardLane lane = RewardLane.Right;

    [Header("Reward")]
    public RewardType type = RewardType.Gold;
    public int amount;

    [Header("ChestDefinotion")]
    public ChestDefinition chest;

    [Header("UnitRarity")]
    public UnitRarity rarity;
}

/// <summary>
/// Single source of truth for all level rewards (main + optional specials).
/// Store as ScriptableObject asset.
/// </summary>
[CreateAssetMenu(menuName = "Game/Progression/Level Rewards Database", fileName = "LevelRewardsDatabase")]
public class LevelRewardsDatabase : ScriptableObject
{
    [Min(1)] public int maxLevel = 30;
    public List<LevelRewardEntry> rewards = new();

    /// <summary>
    /// Returns reward for a specific level+lane, or null if none.
    /// </summary>
    public LevelRewardEntry GetReward(int level, RewardLane lane)
    {
        for (int i = 0; i < rewards.Count; i++)
        {
            var r = rewards[i];
            if (r != null && r.level == level && r.lane == lane)
                return r;
        }
        return null;
    }

    /// <summary>
    /// Key used to store claimed state in GameData (stable and human readable).
    /// </summary>
    public static string MakeClaimKey(int level, RewardLane lane)
    {
        return $"lvl_{level}_lane_{(int)lane}";
    }
}
