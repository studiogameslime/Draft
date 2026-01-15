using UnityEngine;

public enum FortuneRewardType
{
    Chest,
    Gold,
    Gems,
    GreenPart
}

public enum FortuneRewardRarity
{
    Common,
    Rare,
    Epic
}

[System.Serializable]
public class FortuneWheelReward
{
    [Header("Type")]
    public FortuneRewardType type;

    [Header("Rarity")]
    public FortuneRewardRarity rarity;

    [Header("Amount")]
    [Min(0)]
    public int amount = 1;

    [Header("Chest (optional)")]
    public ChestDefinition chestDefinition;
    // Keep it ScriptableObject for now so you can plug your chest asset without forcing a specific class.
}
