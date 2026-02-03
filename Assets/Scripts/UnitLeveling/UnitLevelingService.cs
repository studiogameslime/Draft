using System.Linq;
using UnityEngine;

public static class UnitLevelingService
{
    public const int MaxLevel = 10;

    // Tuning
    public const int BaseUpgradeCost = 20;
    private const float CostGrowth = 1.35f;

    public const float HpDmgPerLevel = 0.10f;     // +10%
    public const float SpawnTimePerLevel = 0.03f; // -3%

    // -------------------------
    // Progress access
    // -------------------------
    public static UnitProgressData GetOrCreateProgress(string unitId)
    {
        if (GameData.Instance == null || GameData.Instance.Save == null) return null;

        var save = GameData.Instance.Save;
        var p = save.ownedUnits.FirstOrDefault(u => u != null && u.unitId == unitId);

        if (p == null)
        {
            p = new UnitProgressData
            {
                unitId = unitId,
                level = 1,
                unlockState = UnitUnlockState.Discovered
            };
            save.ownedUnits.Add(p);
            GameData.Instance.SaveNow();
        }

        if (p.level <= 0) p.level = 1;
        return p;
    }

    public static int GetUnitLevel(string unitId)
    {
        var p = GetOrCreateProgress(unitId);
        return p == null ? 1 : Mathf.Clamp(p.level, 1, MaxLevel);
    }

    // -------------------------
    // Costs
    // -------------------------
    // cost to upgrade from currentLevel -> currentLevel+1
    public static int GetUpgradeCost(int currentLevel)
    {
        currentLevel = Mathf.Clamp(currentLevel, 1, MaxLevel);
        if (currentLevel >= MaxLevel) return 0;

        // Level 1->2 = BaseUpgradeCost
        // Level 2->3 = BaseUpgradeCost * 1.35
        // ...
        float c = BaseUpgradeCost * Mathf.Pow(CostGrowth, currentLevel - 1);
        return Mathf.RoundToInt(c);
    }

    // -------------------------
    // Multipliers
    // -------------------------
    public static float GetHpDamageMultiplier(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        return 1f + (level - 1) * HpDmgPerLevel;
    }

    public static float GetSpawnTimeMultiplier(int level)
    {
        level = Mathf.Clamp(level, 1, MaxLevel);
        return Mathf.Pow(1f - SpawnTimePerLevel, level - 1);
    }

    // -------------------------
    // Upgrade Action
    // -------------------------
    public static bool TryUpgradeUnit(string unitId)
    {
        if (BattleManager.instance != null && BattleManager.instance.IsBattleRunning)
            return false;

        var p = GetOrCreateProgress(unitId);
        if (p == null) return false;

        int cur = Mathf.Clamp(p.level, 1, MaxLevel);
        if (cur >= MaxLevel) return false;

        int cost = GetUpgradeCost(cur);
        if (PlayerCurrencyWallet.Instance == null) return false;

        if (!PlayerCurrencyWallet.Instance.TrySpendScrolls(cost))
            return false;

        p.level = cur + 1;
        GameData.Instance.SaveNow();
        return true;
    }
}
