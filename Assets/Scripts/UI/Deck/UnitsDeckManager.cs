using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitsDeckManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitDefinition[] allUnits;  

    [Header("Deck UI")]
    [SerializeField] private Transform deckRowParent;     
    [SerializeField] private UnitCardView deckCardPrefab; 

    public const int MaxDeckSize = 4;

    private readonly List<UnitDefinition> _deckUnits = new List<UnitDefinition>(MaxDeckSize);

    public System.Action OnDeckChanged;

    private const string DeckSlotKeyPrefix = "deck_slot_";

    public IReadOnlyList<UnitDefinition> CurrentDeck => _deckUnits;
    public IReadOnlyList<UnitDefinition> AllUnits => allUnits;

    private void Awake()
    {
        LoadDeck();
        BuildDeckRow();
    }


    private void LoadDeck()
    {
        _deckUnits.Clear();

        for (int i = 0; i < MaxDeckSize; i++)
        {
            string key = DeckSlotKeyPrefix + i;
            string unitId = PlayerPrefs.GetString(key, string.Empty);
            if (string.IsNullOrEmpty(unitId))
                continue;

            var def = allUnits.FirstOrDefault(u => u != null && u.id == unitId);
            if (def != null)
                _deckUnits.Add(def);
        }

        if (_deckUnits.Count == 0)
        {
            for (int i = 0; i < MaxDeckSize && i < allUnits.Length; i++)
            {
                if (allUnits[i] != null)
                    _deckUnits.Add(allUnits[i]);
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


    public bool IsInDeck(UnitDefinition def)
    {
        if (def == null) return false;
        return _deckUnits.Contains(def);
    }

    public bool IsInDeck(string unitId)
    {
        return _deckUnits.Any(u => u != null && u.id == unitId);
    }

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
            {
                return;
            }

            _deckUnits.Add(def);
        }

        SaveDeck();
        BuildDeckRow();
        OnDeckChanged?.Invoke();
    }

    public void RemoveFromDeck(UnitDefinition def)
    {
        if (def == null) return;

        if (_deckUnits.Remove(def))
        {
            SaveDeck();
            BuildDeckRow();
            OnDeckChanged?.Invoke();
        }
    }


    private void BuildDeckRow()
    {
        foreach (Transform child in deckRowParent)
        {
            Destroy(child.gameObject);
        }

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
        }
    }
}
