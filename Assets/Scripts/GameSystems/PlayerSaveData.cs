using System;
using System.Collections.Generic;
using UnityEditor;
using UnityEngine;

[Serializable]
public class PlayerSaveData
{

    // ==============================
    // PLAYER PROGRESSION (XP / LEVEL)
    // ==============================
    public int playerLevel = 1;
    public int playerXP = 0;

    // ==============================
    // ECONOMY (Currencies)
    // ==============================

    public int gold;
    public int gems;
    public int scrolls;
    public int wheelOfFortuneTokens;


    // future currencies � you can add more later
    // public int eventCoins;
    // public int someOtherCurrency;

    // ==============================
    // Dates/Time
    // ==============================

    public long nextDailyFreeGoldUtcTicks;
    public long lastDailyLoginUtcTicks;
    public long nextHourlyChestUtcTicks;


    // ==============================
    // COLLECTION / UNITS / PARTS
    // ==============================

    // Units the player owns (by unitId)
    public List<UnitProgressData> ownedUnits = new List<UnitProgressData>();

    // Current deck � list of unitIds (4 slots, 8)
    public List<string> currentDeckUnitIds = new List<string>();

    // How many parts the player has per partId
    public List<PartAmountData> parts = new List<PartAmountData>();

    // ==============================
    // PROGRESSION (Stages / Regions)
    // ==============================

    // current stage (could be "stage_5" or an index id)
    public string currentStageId = "1-1";

    // regions that are unlocked (just store ids)
    public List<string> unlockedRegionIds = new List<string>();

    // completed stages(for progress UI)
    public List<string> completedStageIds = new List<string>();

    // ==============================
    // SHOP PURCHASES
    // ==============================

    // what the player bought in the shop (by productId / shopItemId)
    public List<ShopPurchaseData> shopPurchases = new List<ShopPurchaseData>();

    // ==============================
    // CHESTS
    // ==============================

    // how many chests opened in total
    public int totalChestsOpened;

    // how many chests opened by chest type / id
    public List<ChestOpenStatData> chestsOpenedByType = new List<ChestOpenStatData>();

    // ==============================
    // MISSIONS
    // ==============================
    [Header("Missions")]
    // Daily missions
    public List<MissionSaveData> activeDailyMissions = new List<MissionSaveData>();
    public long dailyMissionsLastResetTicks;

    // Weekly missions
    public List<MissionSaveData> activeWeeklyMissions = new List<MissionSaveData>();
    public long weeklyMissionsLastResetTicks;
    
    // how many missions completed (total)
    public int totalMissionsCompleted;

    public bool dailySetRewardGranted;
    public bool weeklySetRewardGranted;


    // in the future you can add: daily/weekly break down if needed

    // ==============================
    // ENEMIES
    // ==============================

    // enemies discovered (enemyId)
    public List<string> discoveredEnemyIds = new List<string>();

    // kills per enemy type
    public List<EnemyKillStatData> enemyKillStats = new List<EnemyKillStatData>();

    // ==============================
    // LEVEL REWARDS (Progression UI)
    // ==============================
    /// <summary>
    /// Keys of claimed rewards. Key format: LevelRewardsDatabase.MakeClaimKey(level, lane)
    /// </summary>
    public List<string> claimedLevelRewards = new List<string>();

    // ==============================
    // BATTLES / SESSIONS
    // ==============================

    // how many games the player played
    public int gamesPlayed;

    // wins / losses
    public int gamesWon;
    public int gamesLost;

    // total play time in seconds (you can convert to minutes/hours in UI)
    public float totalPlayTimeSeconds;

    // small "cool" stats
    public int currentWinStreak;
    public int maxWinStreak;

    // ==============================
    // MASTERY
    // ==============================
    public int masteryDrawCount;                
    public string masteryLastPickedId;          // anti-streak 
    public List<MasteryLevelData> masteryLevels = new List<MasteryLevelData>();

    // ==============================
    // WALL
    // ==============================
    public int wallLevel = 1;

}

// =====================================
// SMALL DATA CLASSES (serializable)
// =====================================

// Unit + its personal progress (level etc.)
public enum UnitUnlockState
{
    Discovered,
    Unlocked,
    Undiscovered
}

[Serializable]
public class UnitProgressData
{
    public string unitId;
    public int level;

    // how many parts/shards for this unit
    public int partsOwned;

    // for �NEW� tag in UI if you want
    public bool isNew;

    public int skillPoints;
    public List<string> unlockedUpgradeNodeIds = new(); // e.g. ["unlock","A1","B1"]

    public UnitUnlockState unlockState;

}

// amount of a generic part (if parts are not per-unit but global)
[Serializable]
public class PartAmountData
{
    public string partId;
    public int amount;
}

// shop purchase info
[Serializable]
public class ShopPurchaseData
{
    public string productId;
    public int timesPurchased;
}

// chest open stats per chest type
[Serializable]
public class ChestOpenStatData
{
    public string chestId;
    public int timesOpened;
}

// enemy kill stats per enemy type
[Serializable]
public class EnemyKillStatData
{
    public string enemyId;
    public int kills;
}

[Serializable]
public class MissionSaveData
{
    public string missionId;
    public int currentProgress;
    public bool completed;
    public bool claimed;
}



[Serializable]
public class MasteryLevelData
{
    public string id;
    public int level;
}




