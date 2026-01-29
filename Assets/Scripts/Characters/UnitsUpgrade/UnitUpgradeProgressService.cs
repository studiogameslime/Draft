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

    public static bool ArePrerequisitesMet(UnitProgressData up, UnitUpgradeNodeDefinition node)
    {
        if (up == null || node == null) return false;
        if (node.prerequisites == null) return true;

        for (int i = 0; i < node.prerequisites.Count; i++)
        {
            var prereq = node.prerequisites[i];
            if (prereq == null) continue;
            if (!IsUnlocked(up, prereq.nodeId)) return false;
        }
        return true;
    }

    public static bool IsAvailableToUnlock(UnitDefinition unit, UnitProgressData up, UnitUpgradeNodeDefinition node)
    {
        if (unit == null || up == null || node == null || !node.IsValid()) return false;
        if (IsUnlocked(up, node.nodeId)) return false;
        return ArePrerequisitesMet(up, node);
    }

    public static bool CanAfford(UnitProgressData up, UnitUpgradeNodeDefinition node)
    {
        if (up == null || node == null) return false;
        return up.skillPoints >= node.skillPointCost;
    }

    public static bool CanUnlock(UnitDefinition unit, UnitProgressData up, UnitUpgradeNodeDefinition node)
    {
        if (!IsAvailableToUnlock(unit, up, node)) return false;
        if (!CanAfford(up, node)) return false;
        return true;
    }

    public static bool TryUnlock(UnitDefinition unit, UnitProgressData up, UnitUpgradeNodeDefinition node)
    {
        if (!CanUnlock(unit, up, node)) return false;

        up.skillPoints -= node.skillPointCost;

        if (up.unlockedUpgradeNodeIds == null)
            up.unlockedUpgradeNodeIds = new List<string>();

        if (!up.unlockedUpgradeNodeIds.Contains(node.nodeId))
            up.unlockedUpgradeNodeIds.Add(node.nodeId);

        if(unit.unlockNode.nodeId == node.nodeId)
        {
            up.unlockState = UnitUnlockState.Unlocked;
        }

        GameData.Instance.SaveNow();


        return true;
    }
}
