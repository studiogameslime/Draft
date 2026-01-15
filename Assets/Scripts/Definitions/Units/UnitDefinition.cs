using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Basic info")]
    public string id;
    public string displayName;
    public string description;
    public UnitRarity rarity;

    [Header("Visuals")]
    public Sprite icon;
    public GameObject prefab;
    public float iconScale = 1f;

    [Header("Classification")]
    public Team unitTeam;
    public UnitClass unitClass;
    public UnitClass targetPriorityClass = UnitClass.None;

    [Header("Souls")]
    public int soulCost;
    [Range(0f, 1f)]
    public float soulDropChance = 0.25f;

    [Header("Stats")]
    public int maxHealth = 100;
    public int damage;
    public float moveSpeed = 2f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public float spawnTime = 5f;

    [Header("Animator")]
    public RuntimeAnimatorController animatorController;

    [Header("Tier / Parts Config")]
    public int maxTier = 3;  

    [System.Serializable]
    public class PartSlotConfig
    {
        public PartSlot slot;          // Head / Body / RightArm...
    }

    public PartSlotConfig[] partSlots;
    public int baseCapacity = 1;

    [Header("This head used for showing enemy count before the round")]
    public GameObject headPrefabForBattlePreview;
}

public enum UnitRarity
{
    Common,
    Rare,
    Epic,
    Legendary
}

public enum PartSlot
{
    Head,
    Body,
    LeftArm,
    RightArm,
    LeftLeg,
    RightLeg,
    Weapon,
    Shield,
    Extra
}