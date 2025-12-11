using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitsDeckManager : MonoBehaviour
{
    public static UnitsDeckManager Instance;

    [Header("Deck UI")]
    [SerializeField] private Transform deckRowParent;
    [SerializeField] private UnitCardView deckCardPrefab;

    public const int MaxDeckSize = 4;

    private readonly List<UnitDefinition> _deckUnits = new List<UnitDefinition>(MaxDeckSize);
    private UnitDefinition[] _allUnitsSource;

    public System.Action OnDeckChanged;
    private const string DeckSlotKeyPrefix = "deck_slot_";

    public IReadOnlyList<UnitDefinition> CurrentDeck => _deckUnits;

    // ----- deck replace mode -----
    private readonly List<UnitCardView> _deckCardViews = new();
    private bool _replaceModeActive = false;
    private UnitDefinition _pendingNewUnit;

    public bool IsReplaceModeActive => _replaceModeActive;

    // ============================
    // UNITY
    // ============================
    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    // ============================
    // INIT
    // ============================
    public void Initialize(UnitDefinition[] allUnits)
    {
        _allUnitsSource = allUnits;
        LoadDeck();
        BuildDeckRow();
    }

    // ============================
    // LOAD / SAVE
    // ============================
    private void LoadDeck()
    {
        _deckUnits.Clear();

        if (_allUnitsSource == null || _allUnitsSource.Length == 0)
        {
            Debug.LogError("UnitsDeckManager: allUnits source is EMPTY");
            return;
        }

        for (int i = 0; i < MaxDeckSize; i++)
        {
            string key = DeckSlotKeyPrefix + i;
            string unitId = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(unitId))
                continue;

            var def = _allUnitsSource.FirstOrDefault(u => u != null && u.id == unitId);
            if (def != null)
                _deckUnits.Add(def);
        }

        if (_deckUnits.Count == 0)
        {
            for (int i = 0; i < MaxDeckSize && i < _allUnitsSource.Length; i++)
            {
                if (_allUnitsSource[i] != null)
                    _deckUnits.Add(_allUnitsSource[i]);
            }
        }
    }

    private void SaveDeck()
    {
        for (int i = 0; i < MaxDeckSize; i++)
        {
            string key = DeckSlotKeyPrefix + i;
            if (i < _deckUnits.Count && _deckUnits[i] != null)
                PlayerPrefs.SetString(key, _deckUnits[i].id);
            else
                PlayerPrefs.DeleteKey(key);
        }
        PlayerPrefs.Save();
    }

    public void ReplaceUnitInDeck(UnitDefinition oldUnit, UnitDefinition newUnit)
    {
        int index = _deckUnits.IndexOf(oldUnit);
        if (index == -1)
            return;

        _deckUnits[index] = newUnit;
        SaveDeck();
        BuildDeckRow();
        OnDeckChanged?.Invoke();
        PlayerDeckProvider.Instance?.ReloadDeck();
    }

    // ============================
    // QUERIES
    // ============================
    public bool IsInDeck(UnitDefinition def)
    {
        return def != null && _deckUnits.Contains(def);
    }

    public bool IsInDeck(string unitId)
    {
        return _deckUnits.Any(u => u != null && u.id == unitId);
    }

    // ============================
    // EDIT DECK
    // ============================
    public void ToggleUnit(UnitDefinition def)
    {
        if (def == null) return;

        if (_deckUnits.Contains(def))
        {
            _deckUnits.Remove(def);
        }
        else
        {
            if (_deckUnits.Count >= MaxDeckSize)
                return;
            _deckUnits.Add(def);
        }

        SaveDeck();
        PlayerDeckProvider.Instance?.ReloadDeck();
        BuildDeckRow();
        OnDeckChanged?.Invoke();
    }

    public void RemoveFromDeck(UnitDefinition def)
    {
        if (def == null) return;

        if (_deckUnits.Remove(def))
        {
            SaveDeck();
            PlayerDeckProvider.Instance?.ReloadDeck();
            BuildDeckRow();
            OnDeckChanged?.Invoke();
        }
    }

    // ============================
    // DECK REPLACE MODE
    // ============================
    public void RegisterDeckCard(UnitCardView card)
    {
        if (!_deckCardViews.Contains(card))
            _deckCardViews.Add(card);
    }

    private void ClearDeckCardViews()
    {
        _deckCardViews.Clear();
    }

    public void StartReplaceMode(UnitDefinition newUnit)
    {
        _pendingNewUnit = newUnit;
        _replaceModeActive = true;
        UpdateDeckReplaceVisuals(true);
    }

    public void CancelReplaceMode()
    {
        _pendingNewUnit = null;
        _replaceModeActive = false;
        UpdateDeckReplaceVisuals(false);
    }

    public void ReplaceWithDeckCard(UnitDefinition deckUnit)
    {
        if (!_replaceModeActive || _pendingNewUnit == null || deckUnit == null)
            return;

        ReplaceUnitInDeck(deckUnit, _pendingNewUnit);
        _pendingNewUnit = null;
        _replaceModeActive = false;
        UpdateDeckReplaceVisuals(false);
    }

    private void UpdateDeckReplaceVisuals(bool active)
    {
        foreach (var card in _deckCardViews)
        {
            if (card != null)
                card.SetReplaceModeVisual(active);
        }
    }

    // ============================
    // UI
    // ============================
    private void BuildDeckRow()
    {
        Debug.Log($"BuildDeckRow {this}");

        if (deckRowParent == null || deckCardPrefab == null)
            return;

        foreach (Transform child in deckRowParent)
            Destroy(child.gameObject);

        ClearDeckCardViews();

        for (int i = 0; i < MaxDeckSize; i++)
        {
            UnitDefinition def = (i < _deckUnits.Count) ? _deckUnits[i] : null;
            var card = Instantiate(deckCardPrefab, deckRowParent);

            if (def != null)
            {
                card.Setup(def, this,
                    isLocked: false,
                    isInDeck: true,
                    isDeckSlot: true);
            }
            else
            {
                card.SetupEmptySlot();
            }

            RegisterDeckCard(card);
        }
    }
}
