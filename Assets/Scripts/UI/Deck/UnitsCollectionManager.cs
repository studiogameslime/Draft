using System.Collections.Generic;
using System.Linq;
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
        unlockedUnitsManager = FindAnyObjectByType<UnlockedUnitsManager>();
        
    }

    private void Start()
    {
        deckManager.Initialize(unitsDatabase.allUnits);
        BuildCollection();
    }

    private void BuildCollection()
    {
        //If there is a card that is expanded (the grid is disabled) collapse it before rerender the collection
        UnitsGridController.Instance.CollapseCurrent();
        _cardsById.Clear();

        foreach (Transform child in collectionParent)
            Destroy(child.gameObject);

        foreach (var def in unitsDatabase.allUnits)
        {
            if (def == null || string.IsNullOrEmpty(def.id))
                continue;

            bool isInDeck = deckManager.IsInDeck(def);

            UnitUnlockState unlockState = UnitUnlockState.Undiscovered;

            var progress = GameData.Instance.Save.ownedUnits
                .FirstOrDefault(u => u.unitId == def.id);

            if (progress != null)
                unlockState = progress.unlockState;

            var card = Instantiate(cardPrefab, collectionParent);

            card.Setup(
                def,
                deckManager,
                unlockState,
                isInDeck,
                isDeckSlot: false
            );

            _cardsById[def.id] = card;
        }
    }


    public void RebuildCollection()
    {
        Debug.Log("RebuildCollection");
        BuildCollection();
    }

}
