using UnityEngine;

/// <summary>
/// Controls the deck panel visibility and interactivity.
/// FIX: Hide/Show must toggle the entire parent container, not only the buttons,
/// otherwise background/frames/placeholders may remain visible.
/// </summary>
public class DeckUIController : MonoBehaviour
{
    [Header("Parent that holds the deck panel content (buttons + visuals)")]
    public Transform cardsParent;   // e.g. UpgragesButtonsContainer

    /// <summary>
    /// Enable/disable all card buttons functionally.
    /// (We do NOT rely on this for hiding the deck.)
    /// </summary>
    public void SetCardsInteractable(bool interactable)
    {
        if (cardsParent == null)
            return;

        var cardButtons = cardsParent.GetComponentsInChildren<UnitSpawnButton>(true);
        foreach (var card in cardButtons)
        {
            if (card != null)
                card.SetCardInteractable(interactable);
        }
    }

    /// <summary>
    /// Shows the entire deck panel.
    /// </summary>
    public void ShowDeck()
    {
        SetDeckActive(true);
    }

    /// <summary>
    /// Hides the entire deck panel.
    /// </summary>
    public void HideDeck()
    {
        SetDeckActive(false);
    }

    private void SetDeckActive(bool active)
    {
        if (cardsParent == null)
            return;

        // IMPORTANT: Toggle the whole panel container.
        // This prevents "empty cards" visuals from staying visible.
        cardsParent.gameObject.SetActive(active);
    }
}
