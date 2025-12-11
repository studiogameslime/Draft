using System.Collections.Generic;
using UnityEngine;

public class UnitsCollectionManager : MonoBehaviour
{
    [Header("Data")]
    [SerializeField] private UnitsDatabase unitsDatabase;

    [Header("Refs")]
    [SerializeField] private UnitsDeckManager deckManager;
    [SerializeField] private UnitCardView cardPrefab;
    [SerializeField] private Transform collectionParent;

    private readonly Dictionary<string, UnitCardView> _cardsById = new();

    private void Start()
    {
        deckManager.Initialize(unitsDatabase.allUnits);

        BuildCollection();


    }



    private void BuildCollection()
    {
        Debug.Log($"BulidCollection {deckManager}");
        _cardsById.Clear();

        foreach (var def in unitsDatabase.allUnits)
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

}

