using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Mastery/Mastery Database", fileName = "MasteryDatabase")]
public class MasteryDatabase : ScriptableObject
{
    [Header("Draw Cost Tuning")]
    public int baseDrawCost = 1000;      // first draw cost
    public float costMultiplier = 1.20f; // growth per draw

    [Header("Rarity Chances (percent)")]
    [Tooltip("Example: Common=70, Rare=25, Epic=5")]
    [Range(0f, 100f)] public float commonChance = 70f;
    [Range(0f, 100f)] public float rareChance = 25f;
    [Range(0f, 100f)] public float epicChance = 5f;

    [Header("Items")]
    public List<MasteryItemDefinition> items = new List<MasteryItemDefinition>();
}
