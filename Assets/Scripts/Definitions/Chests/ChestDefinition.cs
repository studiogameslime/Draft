using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Loot/Chest Definition")]
public class ChestDefinition : ScriptableObject
{
    [Header("Meta")]
    public string id;
    public string displayName;
    public Sprite backgroundImage;
    [Header("Icon for open chests screen")]
    public Sprite chestOpenScreenIcon;
    [Header("Icon for all ui places")]
    public Sprite buttonIcon;
    public ChestRarity chestRarity;
    public RuntimeAnimatorController animatorController;

    [Header("Currency Rewards")]
    public IntRange goldRange;        // Always awarded
    public IntRange diamondsRange;    // Can be 0 - use min=0
    public IntRange scrollRange;

    [Header("Part Drop")]
    [Range(0f, 1f)]
    public float partDropChance = 0.3f;

    [Header("Tokens")]
    [Range(0f, 1f)]
    public float wheelOfFortuneChance = 0.1f;

    [Tooltip("Weights for selecting part rarity if a part drops")]
    public RarityWeight[] partRarityWeights;

}

[System.Serializable]
public struct RarityWeight
{
    public PartRarity rarity;
    public float weight;
}
