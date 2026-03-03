using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Basic info for both unit and enemy")]
    public string id;
    public string displayName;
    public string description;
    public GameObject prefab;
    public UnitRarity rarity;
    public Team unitTeam;
    public UnitClass unitClass;
    public UnitClass targetPriorityClass = UnitClass.None;
    public int maxHealth = 100;
    public int damage;
    public float moveSpeed = 2f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    [Range(0f, 1f)] public float critChance = 0.2f;
    public float critMultiplier = 2f;

    [Header("Audio")]
    public AudioClip attackSound;

    [Header("Fill this for our units only")]
    public Sprite icon;
    public float iconScale = 1f;
    public int soulCost;
    public float spawnTime = 5f;
    public RuntimeAnimatorController animatorController;
    public int maxTier = 3;
    public PartSlotConfig[] partSlots;
    public int baseCapacity = 1;
    public UnitUpgradeNodeDefinition unlockNode;
    public List<UnitUpgradeNodeDefinition> nodes = new();
    public bool disableReserveUnit = false;

    [Header("Fill this for enemies only")]
    [Range(0f, 1f)] public float soulDropChance = 0.25f;
    public GameObject headPrefabForBattlePreview;
    public bool isBoss = false;
    public Sprite HeadWithEyes;



    public UnitUpgradeNodeDefinition GetNode(string nodeId)
    {
        for (int i = 0; i < nodes.Count; i++)
        {
            if (nodes[i] != null && nodes[i].nodeId == nodeId)
                return nodes[i];
        }
        return null;
    }

    [System.Serializable]
    public class PartSlotConfig
    {
        public PartSlot slot;
    }
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