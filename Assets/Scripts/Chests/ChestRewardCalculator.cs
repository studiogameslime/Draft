using UnityEngine;

/// <summary>
/// Responsible for rolling the numeric rewards for a chest:
/// gold, diamonds, wheel tokens, hasPart, partRarity, and ALSO chooses a specific PartDefinition.
/// </summary>
public static class ChestRewardCalculator
{
    private static readonly RarityWeight[] globalFallbackRarityWeights = new[]
    {
        new RarityWeight { rarity = PartRarity.Common, weight = 95f },
        new RarityWeight { rarity = PartRarity.Rare,  weight = 4f  },
        new RarityWeight { rarity = PartRarity.Epic,  weight = 1f  },
    };

    public static ChestReward RollReward(ChestDefinition chest)
    {
        ChestReward reward = new ChestReward();

        if (chest == null)
        {
            Debug.LogError("ChestRewardCalculator.RollReward: chest is null.");
            return reward;
        }

        // 1) Gold is always granted.
        reward.gold = chest.goldRange.GetRandom();

        // 2) Diamonds (can be 0 if range min = 0).
        reward.gems = chest.diamondsRange.GetRandom();

        // 3) Wheel token (0 or 1 for now).
        reward.wheelTokens = 0;
        float tokenChance01 = Mathf.Clamp01(chest.wheelOfFortuneChance);
        if (Random.value <= tokenChance01)
            reward.wheelTokens = 1;

        // Default part values.
        reward.hasPart = false;
        reward.partRarity = PartRarity.Common;
        reward.part = null;

        // 4) Roll if a part drops at all.
        if (Random.value <= chest.partDropChance)
        {
            reward.hasPart = true;

            var weightsToUse = (chest.partRarityWeights != null && chest.partRarityWeights.Length > 0)
                ? chest.partRarityWeights
                : globalFallbackRarityWeights;

            reward.partRarity = RollPartRarity(weightsToUse);

            if (PartDropManager.Instance != null)
                reward.part = PartDropManager.Instance.GetRandomPartForDeckUnits(reward.partRarity);

            if (reward.part == null)
                reward.hasPart = false;
        }

        return reward;
    }

    private static PartRarity RollPartRarity(RarityWeight[] weights)
    {
        if (weights == null || weights.Length == 0)
            return PartRarity.Common;

        float total = 0f;
        for (int i = 0; i < weights.Length; i++)
            total += Mathf.Max(0f, weights[i].weight);

        if (total <= 0f)
            return PartRarity.Common;

        float roll = Random.value * total;
        float cumulative = 0f;

        for (int i = 0; i < weights.Length; i++)
        {
            float w = Mathf.Max(0f, weights[i].weight);
            cumulative += w;
            if (roll <= cumulative)
                return weights[i].rarity;
        }

        return weights[weights.Length - 1].rarity;
    }
}
