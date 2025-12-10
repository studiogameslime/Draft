using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlayerDeckProvider : MonoBehaviour
{
    public static PlayerDeckProvider Instance;

    [Header("All Units Database")]
    [SerializeField] private UnitsDatabase unitsDatabase;

    public List<UnitDefinition> CurrentDeck { get; private set; } = new();

    private const int MaxDeckSize = 4;
    private const string DeckSlotKeyPrefix = "deck_slot_";

    private void Awake()
    {
        if (Instance != null)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadDeck();
    }

    public void Initialize(UnitDefinition[] allUnits)
    {
        unitsDatabase.allUnits = allUnits;
        LoadDeck();
    }

    public void ReloadDeck()
    {
        LoadDeck();
    }

    private void LoadDeck()
    {
        CurrentDeck.Clear();

        if (unitsDatabase.allUnits == null || unitsDatabase.allUnits.Length == 0)
        {
            Debug.LogError("PlayerDeckProvider: Initialize() not get allUnits.");
            return;
        }

        for (int i = 0; i < MaxDeckSize; i++)
        {
            string key = DeckSlotKeyPrefix + i;
            string unitId = PlayerPrefs.GetString(key, "");

            if (string.IsNullOrEmpty(unitId))
                continue;

            var def = unitsDatabase.allUnits.FirstOrDefault(u => u != null && u.id == unitId);
            if (def != null)
                CurrentDeck.Add(def);
        }

        Debug.Log("PlayerDeck Reloaded: " + CurrentDeck.Count);
    }

}
