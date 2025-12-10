using UnityEngine;

public class UnitSelectionUI : MonoBehaviour
{
    [Header("UI")]
    public Transform buttonsParent;
    public UnitSpawnButton buttonPrefab;

    [HideInInspector]
    public BattleManager battleManager;

    public void RollNewUnits()
    {
        Debug.Log("Provider = " + PlayerDeckProvider.Instance);
        if (PlayerDeckProvider.Instance != null)
        {
            Debug.Log("Deck count = " + PlayerDeckProvider.Instance.CurrentDeck.Count);
        }

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

        foreach (var unit in deck)
        {
            UnitSpawnButton btn = Instantiate(buttonPrefab, buttonsParent);
            btn.Init(unit, this);
        }

        Debug.Log("Rolled Units FROM PLAYER DECK");
    }
}
