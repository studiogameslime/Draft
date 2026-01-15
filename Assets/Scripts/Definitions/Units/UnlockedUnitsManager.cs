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
    }

    public void UnlockUnit(UnitDefinition unit, bool save = true)
    {
        if (unit == null || string.IsNullOrEmpty(unit.id))
            return;

        // כבר פתוח
        if (_unlockedIds.Contains(unit.id))
            return;

        _unlockedIds.Add(unit.id);

        if (!_unlockedUnits.Contains(unit))
            _unlockedUnits.Add(unit);

        EnsureOwnedEntry(unit.id);

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
    }
}
