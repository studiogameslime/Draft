using UnityEngine;

public enum MissionType
{
    Daily,
    Weekly
}

public enum MissionAction
{
    WinBattles,
    PlayBattles,
    CollectParts,
    UpgradePart,
    PlayWithUnitClass,
    PlayWithSpecificUnit,
    OpenChests,
    DmgToEnemyUnits,
    KillEnemyUnits,
    SpendGems,
    SpendGold,
    Login,
    SpinWheelOfFortune,
    KillSpecificEnemyUnit
}

[CreateAssetMenu(menuName = "Missions/Mission Definition")]
public class MissionDefinition : ScriptableObject
{
    [Header("Meta")]
    public string id;
    public string title;
    public string description;
    public MissionType missionType;

    [Header("Condition")]
    public MissionAction action;
    public int targetAmount;

    [Header("Optional Filters")]
    public UnitClass requiredUnitClass; // None = not rallevent
    public UnitDefinition requiredUnit; // null = not relevant


    [Header("Reward")]
    public int goldReward;
    public int gemsReward;
    public int partsReward;
    public ChestDefinition chestReward;
}
