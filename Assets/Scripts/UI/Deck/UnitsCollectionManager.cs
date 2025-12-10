using System.Collections.Generic;
using UnityEngine;

public class UnitsCollectionManager : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private UnitsDeckManager deckManager;
    [SerializeField] private UnitCardView cardPrefab;
    [SerializeField] private Transform collectionParent;

    private readonly Dictionary<string, UnitCardView> _cardsById = new();

    private void Start()
    {
        BuildCollection();
        deckManager.OnDeckChanged += RefreshDeckHighlights;
    }

    private void OnDestroy()
    {
        if (deckManager != null)
            deckManager.OnDeckChanged -= RefreshDeckHighlights;
    }

    private void BuildCollection()
    {
        _cardsById.Clear();

        foreach (var def in deckManager.AllUnits)
        {
            if (def == null || string.IsNullOrEmpty(def.id))
                continue;

            bool isLocked = false; 
            bool isInDeck = deckManager.IsInDeck(def);

            var card = Instantiate(cardPrefab, collectionParent);
            card.Setup(def, deckManager, isLocked, isInDeck, isDeckSlot: false);

            _cardsById[def.id] = card;
        }
    }

    private void RefreshDeckHighlights()
    {
        foreach (var kvp in _cardsById)
        {
            bool inDeck = deckManager.IsInDeck(kvp.Key);
            kvp.Value.SetDeckState(inDeck);
        }
    }
}
