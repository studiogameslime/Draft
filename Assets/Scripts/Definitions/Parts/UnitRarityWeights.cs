using UnityEngine;

[CreateAssetMenu(menuName = "Drops/Unit Rarity Weights")]
public class UnitRarityWeights : ScriptableObject
{
    [Header("Weight per unit rarity (higher = more common)")]
    public float commonWeight = 4f;
    public float rareWeight = 2f;
    public float epicWeight = 1f;
    public float legendaryWeight = 0.5f;

    public float GetWeight(UnitRarity rarity)
    {
        switch (rarity)
        {
            case UnitRarity.Common: return commonWeight;
            case UnitRarity.Rare: return rareWeight;
            case UnitRarity.Epic: return epicWeight;
            case UnitRarity.Legendary: return legendaryWeight;
            default: return 1f;
        }
    }
}
