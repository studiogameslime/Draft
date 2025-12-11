using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UnitCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject emptySlotVisual;

    private UnitDefinition _definition;
    private UnitsDeckManager _deckManager;
    private bool _isDeckSlot;

    public void Setup(UnitDefinition def, UnitsDeckManager deckManager,
        bool isLocked, bool isInDeck, bool isDeckSlot)
    {
        _definition = def;
        _deckManager = deckManager;
        _isDeckSlot = isDeckSlot;

        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(false);

        iconImage.sprite = def.icon;
        nameText.text = def.displayName;

        gameObject.SetActive(true);
    }

    public void SetupEmptySlot()
    {
        _definition = null;
        _isDeckSlot = true;

        if (iconImage != null) iconImage.sprite = null;
        nameText.text = "";
        if (emptySlotVisual != null) emptySlotVisual.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_definition == null) return;

        UnitDetailsPopupController.Instance.Open(
            _definition,
            isDeckSlot: _isDeckSlot,
            deckManager: _deckManager
        );
    }
}
