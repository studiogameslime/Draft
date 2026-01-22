using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public interface IUnlockedUnitsProvider
{
    IReadOnlyList<UnitDefinition> GetUnlockedUnits();
    bool IsUnlocked(UnitDefinition unit);
}

public class UnlockedUnitsManager : MonoBehaviour, IUnlockedUnitsProvider
{
    [SerializeField] private UnitsUnlockConfig config;

    private readonly List<UnitDefinition> _unlockedUnits = new();
    private readonly HashSet<string> _unlockedIds = new();

    public IReadOnlyList<UnitDefinition> GetUnlockedUnits() => _unlockedUnits;

    public bool IsUnlocked(UnitDefinition unit)
        => unit != null && !string.IsNullOrEmpty(unit.id) && _unlockedIds.Contains(unit.id);

    private void Awake()
    {
        StartCoroutine(InitRoutine());
    }

    private IEnumerator InitRoutine()
    {
        while (GameData.Instance == null || GameData.Instance.Save == null)
            yield return null;

        RebuildFromSave();

        if (_unlockedIds.Count == 0 && config != null && config.startingUnits != null)
        {
            foreach (var u in config.startingUnits)
                UnlockUnit(u, save: false);

            SaveToGameData();
        }
        else
        {
            // אם זה לא משחק חדש אבל תרצה לוודא שלכולם יש unlockNode פתוח (בטוח ולא הורס),
            // אפשר להשאיר את זה דולק:
            EnsureUnlockSkillForAllUnlockedUnits(save: true);
        }
    }

    public void UnlockUnit(UnitDefinition unit, bool save = true)
    {
        if (unit == null || string.IsNullOrEmpty(unit.id))
            return;

        if (_unlockedIds.Contains(unit.id))
            return;

        _unlockedIds.Add(unit.id);

        if (!_unlockedUnits.Contains(unit))
            _unlockedUnits.Add(unit);

        EnsureOwnedEntry(unit.id);

        // חדש: פותח לכל יחידה את ה-unlock node הראשון אוטומטית
        EnsureDefaultUnlockSkill(unit);

        if (save)
            SaveToGameData();
    }

    private void EnsureOwnedEntry(string unitId)
    {
        var save = GameData.Instance.Save;

        if (save.ownedUnits == null)
            save.ownedUnits = new List<UnitProgressData>();

        bool exists = save.ownedUnits.Any(u => u != null && u.unitId == unitId);
        if (exists) return;

        save.ownedUnits.Add(new UnitProgressData
        {
            unitId = unitId,
            level = 1,
            partsOwned = 0,
            isNew = true
        });
    }

    private void EnsureDefaultUnlockSkill(UnitDefinition unit)
    {
        if (unit == null || string.IsNullOrEmpty(unit.id))
            return;

        // מאיפה להביא את ה-node של ה-unlock?
        // 1) הכי נכון: unit.unlockNode
        // 2) fallback: nodeId == "unlock"
        // 3) fallback: tier == 0 (אם tier הוא int)
        UnitUpgradeNodeDefinition unlockNode = unit.unlockNode;

        if (unlockNode == null && unit.nodes != null)
            unlockNode = unit.nodes.FirstOrDefault(n => n != null && n.nodeId == "unlock");

        if (unlockNode == null && unit.nodes != null)
            unlockNode = unit.nodes.FirstOrDefault(n => n != null && (int)n.tier == 0);

        if (unlockNode == null || string.IsNullOrEmpty(unlockNode.nodeId))
            return;

        var save = GameData.Instance.Save;
        if (save.ownedUnits == null)
            save.ownedUnits = new List<UnitProgressData>();

        var progress = save.ownedUnits.FirstOrDefault(u => u != null && u.unitId == unit.id);
        if (progress == null)
            return;

        if (progress.unlockedUpgradeNodeIds == null)
            progress.unlockedUpgradeNodeIds = new List<string>();

        if (!progress.unlockedUpgradeNodeIds.Contains(unlockNode.nodeId))
            progress.unlockedUpgradeNodeIds.Add(unlockNode.nodeId);
    }

    private void EnsureUnlockSkillForAllUnlockedUnits(bool save)
    {
        if (config == null || config.allUnits == null)
            return;

        foreach (var def in _unlockedUnits)
            EnsureDefaultUnlockSkill(def);

        if (save)
            SaveToGameData();
    }

    private void RebuildFromSave()
    {
        _unlockedUnits.Clear();
        _unlockedIds.Clear();

        if (config == null || config.allUnits == null)
            return;

        var save = GameData.Instance.Save;

        if (save.ownedUnits == null)
            save.ownedUnits = new List<UnitProgressData>();

        foreach (var p in save.ownedUnits)
        {
            if (p == null || string.IsNullOrEmpty(p.unitId))
                continue;

            _unlockedIds.Add(p.unitId);

            var def = config.allUnits.FirstOrDefault(u => u != null && u.id == p.unitId);
            if (def != null)
                _unlockedUnits.Add(def);
        }
    }

    private void SaveToGameData()
    {
        GameData.Instance.SaveNow();
    }

    public void ReloadFromSave()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null) return;
        RebuildFromSave();
        EnsureUnlockSkillForAllUnlockedUnits(save: true);
    }
}
