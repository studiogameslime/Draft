using UnityEngine;

public enum MasteryRarity { Common, Rare, Epic }
public enum MasteryValueKind { Percent, Flat }

public enum MasteryStat
{
    GlobalDamagePercent,
    GlobalHpPercent,
    AttackSpeedPercent,
    MoveSpeedPercent,
    GoldGainPercent,
    DailyChestFlat,
    DailyFreeUpgradeFlat,
    SoulsDropPercent,
    GoldPerKillFlat,
    GemsCostReduction,
    GoldCostReduction,
    SoulsOverflow,
    HourlyChestCooldownReduction,
    WallMaxHpPercent,
    WallHealPercent
}

[CreateAssetMenu(menuName = "Mastery/Mastery Item Definition", fileName = "MasteryItem_")]
public class MasteryItemDefinition : ScriptableObject
{
    [Header("Identity")]
    public string id;                 // MUST be unique + stable (for save)
    public string displayName;
    [TextArea] public string description;

    [Header("Presentation")]
    public MasteryRarity rarity;
    public Sprite icon;
    [Range(0.5f, 1.5f)] public float iconScale = 1f; // ���� �� ����� scale ��� ��������

    [Header("Effect")]
    public MasteryStat stat;
    public MasteryValueKind valueKind = MasteryValueKind.Percent;

    [Tooltip("Added per level. Percent means '+0.5' => +0.5% each level")]
    public float valuePerLevel = 0.5f;

    public int maxLevel;


    public float GetValueAtLevel(int level)
    {
        if (level <= 0)
            return 0f;

        // Level 1 gives valuePerLevel
        return valuePerLevel * level;
    }

}
