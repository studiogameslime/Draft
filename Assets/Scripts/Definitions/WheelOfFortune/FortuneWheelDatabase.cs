using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Fortune Wheel/Wheel Database", fileName = "FortuneWheelDatabase")]
public class FortuneWheelDatabase : ScriptableObject
{
    [Header("Slots (2 to 12)")]
    public List<FortuneWheelSlotDefinition> slots = new List<FortuneWheelSlotDefinition>();
    public int Count => slots != null ? slots.Count : 0;

    [Header("Rarity Chances (percent)")]
    [Range(0f, 100f)] public float commonChance = 70f;
    [Range(0f, 100f)] public float rareChance = 25f;
    [Range(0f, 100f)] public float epicChance = 5f;

    // Returns normalized chances. If total is 0, falls back to equal chances.
    public void GetNormalizedRarityChances(out float c, out float r, out float e)
    {
        c = Mathf.Max(0f, commonChance);
        r = Mathf.Max(0f, rareChance);
        e = Mathf.Max(0f, epicChance);

        float total = c + r + e;
        if (total <= 0.0001f)
        {
            c = r = e = 1f / 3f;
            return;
        }

        c /= total;
        r /= total;
        e /= total;
    }
}


[System.Serializable]
public class FortuneWheelRarityChances
{
    [Range(0f, 100f)]
    public float common = 70f;

    [Range(0f, 100f)]
    public float rare = 25f;

    [Range(0f, 100f)]
    public float epic = 5f;

    // Optional helper (לא חובה להשתמש עכשיו)
    public float GetChance(FortuneRewardRarity rarity)
    {
        switch (rarity)
        {
            case FortuneRewardRarity.Common: return common;
            case FortuneRewardRarity.Rare: return rare;
            case FortuneRewardRarity.Epic: return epic;
            default: return 0f;
        }
    }
}