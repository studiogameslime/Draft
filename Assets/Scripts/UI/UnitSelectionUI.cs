using UnityEngine;

/// <summary>
/// Responsible for building the deck buttons UI from the player's current deck.
/// </summary>
public class UnitSelectionUI : MonoBehaviour
{
    [Header("UI")]
    public Transform buttonsParent;
    public UnitSpawnButton buttonPrefab;

    [HideInInspector]
    public BattleManager battleManager;

    public void RollNewUnits()
    {
        // Ensure DeckUIController points to the correct parent that holds the buttons.
        // This is important when DeckUIController is in another scene.
        if (battleManager != null && battleManager.deckUI != null)
            battleManager.deckUI.cardsParent = buttonsParent;

        // Clear old buttons.
        foreach (Transform child in buttonsParent)
            Destroy(child.gameObject);

        if (PlayerDeckProvider.Instance == null)
        {
            Debug.LogError("PlayerDeckProvider NOT FOUND");
            return;
        }

        var deck = PlayerDeckProvider.Instance.CurrentDeck;
        if (deck == null || deck.Count == 0)
        {
            Debug.LogError("Deck is EMPTY");
            return;
        }

        // Build buttons.
        foreach (var unit in deck)
        {
            UnitSpawnButton btn = Instantiate(buttonPrefab, buttonsParent);
            btn.Init(unit, this);
        }

        // Make sure the deck panel is visible/hidden by DeckUIController logic,
        // not by leaving some placeholders enabled.
        if (battleManager != null && battleManager.deckUI != null)
            battleManager.deckUI.HideDeck();
    }
}
