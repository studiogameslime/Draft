// UnitUpgradeProgressService.cs
using System.Collections.Generic;
using UnityEngine;

public static class UnitUpgradeProgressService
{
    public static UnitProgressData GetOrCreateUnitProgress(string unitId)
    {
        var save = GameData.Instance.Save;
        if (save == null) return null;

        for (int i = 0; i < save.ownedUnits.Count; i++)
        {
            if (save.ownedUnits[i] != null && save.ownedUnits[i].unitId == unitId)
                return save.ownedUnits[i];
        }

        var up = new UnitProgressData { unitId = unitId };
        save.ownedUnits.Add(up);
        GameData.Instance.SaveNow();
        return up;
    }

    public static bool IsUnlocked(UnitProgressData up, string nodeId)
    {
        if (up == null || up.unlockedUpgradeNodeIds == null) return false;
        return up.unlockedUpgradeNodeIds.Contains(nodeId);
    }

    // CHANGED: UnitDefinition instead of UnitUpgradeTreeDefinition
    public static bool CanUnlock(UnitDefinition unit, UnitProgressData up, UnitUpgradeNodeDefinition node)
    {
        if (unit == null || up == null || node == null || !node.IsValid()) return false;
        if (IsUnlocked(up, node.nodeId)) return false;
        if (up.skillPoints < node.skillPointCost) return false;

        // all prereqs must be unlocked
        if (node.prerequisites != null)
        {
            for (int i = 0; i < node.prerequisites.Count; i++)
            {
                var prereq = node.prerequisites[i];
                if (prereq == null) continue;
                if (!IsUnlocked(up, prereq.nodeId)) return false;
            }
        }
        return true;
    }

    // CHANGED: UnitDefinition instead of UnitUpgradeTreeDefinition
    public static bool TryUnlock(UnitDefinition unit, UnitProgressData up, UnitUpgradeNodeDefinition node)
    {
        if (!CanUnlock(unit, up, node)) return false;

        up.skillPoints -= node.skillPointCost;

        if (up.unlockedUpgradeNodeIds == null)
            up.unlockedUpgradeNodeIds = new List<string>();

        if (!up.unlockedUpgradeNodeIds.Contains(node.nodeId))
            up.unlockedUpgradeNodeIds.Add(node.nodeId);

        GameData.Instance.SaveNow();
        return true;
    }
}
