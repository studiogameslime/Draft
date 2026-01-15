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
    private const string PlayerPrefsKey = "UnlockedUnits";

    public IReadOnlyList<UnitDefinition> GetUnlockedUnits() => _unlockedUnits;
    public bool IsUnlocked(UnitDefinition unit) => _unlockedUnits.Contains(unit);

    private void Awake()
    {
        
        Load();
        if (_unlockedUnits.Count == 0 && config != null)
        {
            foreach (var u in config.startingUnits)
                UnlockUnit(u, false);
            Save();
        }
    }

    public void UnlockUnit(UnitDefinition unit, bool save = true)
    {
        if (unit == null || _unlockedUnits.Contains(unit)) return;
        _unlockedUnits.Add(unit);
        if (save) Save();
    }

    private void Save()
    {
        var ids = _unlockedUnits.Where(u => u != null).Select(u => u.id);
        PlayerPrefs.SetString(PlayerPrefsKey, string.Join("|", ids));
        PlayerPrefs.Save();
    }

    private void Load()
    {
        _unlockedUnits.Clear();
        if (config == null || config.allUnits == null) return;

        if (!PlayerPrefs.HasKey(PlayerPrefsKey)) return;

        string data = PlayerPrefs.GetString(PlayerPrefsKey);
        var ids = data.Split('|');
        foreach (var id in ids)
        {
            var unit = config.allUnits.FirstOrDefault(u => u != null && u.id == id);
            if (unit != null)
                _unlockedUnits.Add(unit);
        }
    }
}
