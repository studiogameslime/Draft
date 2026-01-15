using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerDeckProvider : MonoBehaviour
{
    public static PlayerDeckProvider Instance;

    [Header("All Units Database")]
    [SerializeField] private UnitsDatabase unitsDatabase;

    [Header("Starting Deck Config")]
    [SerializeField] private UnitsUnlockConfig unlockConfig;

    public List<UnitDefinition> CurrentDeck { get; private set; } = new();

    private const int MaxDeckSize = 4;

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);

        InitializeFirstTime();   // make sure first deck exists in GameData
        LoadDeck();              // build CurrentDeck from GameData
    }

    /// <summary>
    /// On first launch (or if no deck saved yet),
    /// set starting deck into GameData.Save.currentDeckUnitIds.
    /// </summary>
    private void InitializeFirstTime()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null)
        {
            Debug.LogError("PlayerDeckProvider: GameData or Save is null.");
            return;
        }

        var save = GameData.Instance.Save;

        // If deck already has units saved, do nothing.
        if (save.currentDeckUnitIds != null && save.currentDeckUnitIds.Count > 0)
            return;

        if (unlockConfig == null || unlockConfig.startingUnits == null || unlockConfig.startingUnits.Length == 0)
        {
            Debug.LogWarning("PlayerDeckProvider: No starting units assigned in UnlockConfig!");
            return;
        }

        // Ensure the list exists
        if (save.currentDeckUnitIds == null)
            save.currentDeckUnitIds = new List<string>();
        else
            save.currentDeckUnitIds.Clear();

        // Take up to MaxDeckSize starting units
        for (int i = 0; i < unlockConfig.startingUnits.Length && i < MaxDeckSize; i++)
        {
            var unit = unlockConfig.startingUnits[i];
            if (unit != null)
                save.currentDeckUnitIds.Add(unit.id);
        }

        GameData.Instance.SaveNow();
        Debug.Log("PlayerDeckProvider: Starting units assigned for first launch via GameData.");
    }

    public void ReloadDeck()
    {
        LoadDeck();
    }

    /// <summary>
    /// Builds CurrentDeck list from GameData.Save.currentDeckUnitIds.
    /// </summary>
    private void LoadDeck()
    {
        CurrentDeck.Clear();

        if (GameData.Instance == null || GameData.Instance.Save == null)
        {
            Debug.LogError("PlayerDeckProvider: GameData or Save is null.");
            return;
        }

        if (unitsDatabase == null || unitsDatabase.allUnits == null || unitsDatabase.allUnits.Length == 0)
        {
            Debug.LogError("PlayerDeckProvider: unitsDatabase.allUnits is empty or not assigned.");
            return;
        }

        var savedIds = GameData.Instance.Save.currentDeckUnitIds;
        if (savedIds == null || savedIds.Count == 0)
        {
            Debug.Log("PlayerDeckProvider: No deck ids found in GameData.");
            return;
        }

        foreach (var unitId in savedIds)
        {
            if (string.IsNullOrEmpty(unitId))
                continue;

            var def = unitsDatabase.allUnits.FirstOrDefault(u => u != null && u.id == unitId);
            if (def != null)
                CurrentDeck.Add(def);
        }

        // Optional: debug
        // Debug.Log("PlayerDeckProvider: Loaded deck: " + string.Join(", ", CurrentDeck.Select(u => u.displayName)));
    }
}
