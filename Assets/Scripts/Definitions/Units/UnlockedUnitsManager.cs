using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnlockedUnitsManager : MonoBehaviour
{
    public static UnlockedUnitsManager Instance;

    [SerializeField] private UnitsUnlockConfig config;

    private readonly List<UnitDefinition> _unlockedUnits = new();
    private readonly HashSet<string> _unlockedIds = new();

    public IReadOnlyList<UnitDefinition> GetUnlockedUnits() => _unlockedUnits;

    public bool IsUnlocked(UnitDefinition unit)
        => unit != null &&
           !string.IsNullOrEmpty(unit.id) &&
           _unlockedIds.Contains(unit.id);

    private void Awake()
    {
        Instance = this;
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
                UnlockUnitCompletely(u, save: false);

            GameData.Instance.SaveNow();
        }
    }

    // -----------------------
    // DISCOVERY
    // -----------------------

    public bool IsDiscovered(UnitDefinition unit)
    {
        if (unit == null) return false;

        var progress = GetProgress(unit.id);
        return progress != null;
    }

    public void DiscoverUnit(UnitDefinition unit, bool save = true)
    {
        if (unit == null || string.IsNullOrEmpty(unit.id))
            return;

        var saveData = GameData.Instance.Save;
        if (saveData.ownedUnits.Any(u => u.unitId == unit.id))
            return;

        saveData.ownedUnits.Add(new UnitProgressData
        {
            unitId = unit.id,
            level = 1,
            partsOwned = 0,
            isNew = true,
            skillPoints = 0,
            unlockState = UnitUnlockState.Discovered,
            unlockedUpgradeNodeIds = new List<string>()
        });

        if (save)
            GameData.Instance.SaveNow();
    }

    public UnitDefinition DiscoverRandomUnitByRarity(UnitRarity rarity)
    {
        if (config == null || config.allUnits == null || config.allUnits.Length == 0)
            return null;

        if (GameData.Instance == null || GameData.Instance.Save == null)
            return null;

        var save = GameData.Instance.Save;

        if (save.ownedUnits == null)
            save.ownedUnits = new List<UnitProgressData>();

        // Collect undiscovered units of the requested rarity
        List<UnitDefinition> candidates = new List<UnitDefinition>();

        foreach (var unit in config.allUnits)
        {
            if (unit == null)
                continue;

            if (unit.rarity != rarity)
                continue;

            bool alreadyDiscovered = save.ownedUnits.Any(u => u != null && u.unitId == unit.id);
            if (alreadyDiscovered)
                continue;

            candidates.Add(unit);
        }

        if (candidates.Count == 0)
            return null;

        // Pick random undiscovered unit
        UnitDefinition picked = candidates[Random.Range(0, candidates.Count)];

        // Mark as discovered (but NOT unlocked)
        save.ownedUnits.Add(new UnitProgressData
        {
            unitId = picked.id,
            level = 1,
            partsOwned = 0,
            isNew = true,
            skillPoints = 0,
            unlockedUpgradeNodeIds = new List<string>() // unlock skill NOT added here
        });

        GameData.Instance.SaveNow();

        // Refresh runtime cache
        RebuildFromSave();
        FindAnyObjectByType<UnitsCollectionManager>()?.RebuildCollection();

        return picked;
    }


    // -----------------------
    // FULL UNLOCK
    // -----------------------

    public void UnlockUnitCompletely(UnitDefinition unit, bool save = true)
    {
        if (unit == null || string.IsNullOrEmpty(unit.id))
            return;

        var progress = GetOrCreateProgress(unit.id);
        progress.unlockState = UnitUnlockState.Unlocked;

        EnsureDefaultUnlockSkill(unit, progress);

        if (!_unlockedIds.Contains(unit.id))
            _unlockedIds.Add(unit.id);

        if (!_unlockedUnits.Contains(unit))
            _unlockedUnits.Add(unit);

        if (save)
            GameData.Instance.SaveNow();
    }

    // -----------------------
    // INTERNAL
    // -----------------------

    private UnitProgressData GetProgress(string unitId)
    {
        return GameData.Instance.Save.ownedUnits
            .FirstOrDefault(u => u.unitId == unitId);
    }

    private UnitProgressData GetOrCreateProgress(string unitId)
    {
        var progress = GetProgress(unitId);
        if (progress != null)
            return progress;

        progress = new UnitProgressData
        {
            unitId = unitId,
            level = 1,
            unlockState = UnitUnlockState.Discovered,
            unlockedUpgradeNodeIds = new List<string>()
        };

        GameData.Instance.Save.ownedUnits.Add(progress);
        return progress;
    }

    private void EnsureDefaultUnlockSkill(UnitDefinition unit, UnitProgressData progress)
    {
        if (unit.unlockNode == null)
            return;

        if (!progress.unlockedUpgradeNodeIds.Contains(unit.unlockNode.nodeId))
            progress.unlockedUpgradeNodeIds.Add(unit.unlockNode.nodeId);
    }

    private void RebuildFromSave()
    {
        _unlockedUnits.Clear();
        _unlockedIds.Clear();

        if (config == null || config.allUnits == null)
            return;

        foreach (var p in GameData.Instance.Save.ownedUnits)
        {
            if (p.unlockState != UnitUnlockState.Unlocked)
                continue;

            var def = config.allUnits.FirstOrDefault(u => u.id == p.unitId);
            if (def == null)
                continue;

            _unlockedIds.Add(def.id);
            _unlockedUnits.Add(def);
        }
    }
}
