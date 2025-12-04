using UnityEngine;

public class DeckUIController : MonoBehaviour
{
    [Header("Parent that holds all the card buttons")]
    public Transform cardsParent;   // e.g. UpgragesButtonsContainer

    /// <summary>
    /// Enable/disable all card buttons visually and functionally.
    /// </summary>
    public void SetCardsInteractable(bool interactable)
    {
        if (cardsParent == null)
            return;

        // Find all current UnitSpawnButton under the parent (even if created at runtime)
        var cardButtons = cardsParent.GetComponentsInChildren<UnitSpawnButton>(true);

        foreach (var card in cardButtons)
        {
            if (card != null)
                card.SetCardInteractable(interactable);
        }
    }
}
