using System.Collections.Generic;
using UnityEngine;

public enum UpgradeTier { Unlock = 0, Tier1 = 1, Tier2 = 2, Tier3 = 3, Tier4 = 4 }
public enum UpgradeLane { Center = 0, Left = 1, Right = 2 }

[CreateAssetMenu(menuName = "Game/Units/Upgrade Node", fileName = "UpgradeNode_")]
public class UnitUpgradeNodeDefinition : ScriptableObject
{
    [Header("Identity")]
    public string nodeId;                 // e.g. "unlock", "A1", "B2"
    [Min(0)] public int tier;   // 0 = Unlock, 1 = Tier1, 2 = Tier2 ... 
    public UpgradeLane lane;

    [Header("UI")]
    public string title;
    public string description;
    public Sprite icon;

    [Header("Cost")]
    [Min(0)] public int skillPointCost = 1;
    public int soulsCost;

    [Header("Prerequisites")]
    public List<UnitUpgradeNodeDefinition> prerequisites = new(); // e.g. A2 prereq = A1

    // UnitUpgradeNodeDefinition
    [Header("Skill Effect")]
    public UnitSkillBehaviour skillEffect;  
    [Range(0f, 1f)] public float skillPercentValue;    


    public bool IsValid() => !string.IsNullOrEmpty(nodeId);
}
