using System.Collections.Generic;
using UnityEngine;

public class UnitsCollectionManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsDatabase unitsDatabase;

    [Header("Refs")]
    private UnitsDeckManager deckManager;
    [SerializeField] private UnitCardView cardPrefab;
    [SerializeField] private Transform collectionParent;
    [SerializeField] private UnlockedUnitsManager unlockedUnitsManager; 

    private readonly Dictionary<string, UnitCardView> _cardsById = new();

    private void Awake()
    {
        deckManager = FindAnyObjectByType<UnitsDeckManager>();
    }

    private void Start()
    {
        deckManager.Initialize(unitsDatabase.allUnits);
        BuildCollection();
    }

    private void BuildCollection()
    {
        _cardsById.Clear();

        foreach (Transform child in collectionParent)
            Destroy(child.gameObject);

        foreach (var def in unitsDatabase.allUnits)
        {
            if (def == null || string.IsNullOrEmpty(def.id))
                continue;

            bool isInDeck = deckManager.IsInDeck(def);

            bool isLocked = false;
            if (unlockedUnitsManager != null)
            {
                isLocked = !unlockedUnitsManager.IsUnlocked(def);
            }

            var card = Instantiate(cardPrefab, collectionParent);
            
            card.Setup(def, deckManager, isLocked, isInDeck, isDeckSlot: false);

            _cardsById[def.id] = card;
        }
    }
}
