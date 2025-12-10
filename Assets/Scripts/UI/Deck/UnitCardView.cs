using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;

public class UnitCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;

    [Header("States")]
    [SerializeField] private GameObject lockedOverlay;  
    [SerializeField] private GameObject deckHighlight;  
    [SerializeField] private GameObject emptySlotVisual; 

    private UnitDefinition _definition;
    private UnitsDeckManager _deckManager;
    private bool _isLocked;
    private bool _isDeckSlot; 

    public UnitDefinition Definition => _definition;

    // ------------------ Setup ------------------

    public void Setup(UnitDefinition def, UnitsDeckManager deckManager,
        bool isLocked, bool isInDeck, bool isDeckSlot)
    {
        _definition = def;
        _deckManager = deckManager;
        _isLocked = isLocked;
        _isDeckSlot = isDeckSlot;

        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(false);

        if (iconImage != null)
            iconImage.sprite = def.icon;

        if (nameText != null)
            nameText.text = def.displayName;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(isLocked);

        if (deckHighlight != null)
            deckHighlight.SetActive(isInDeck && !isLocked);

        gameObject.SetActive(true);
    }

   
    public void SetupEmptySlot()
    {
        _definition = null;
        _isLocked = false;
        _isDeckSlot = true;

        if (iconImage != null)
            iconImage.sprite = null;

        if (nameText != null)
            nameText.text = string.Empty;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(false);

        if (deckHighlight != null)
            deckHighlight.SetActive(false);

        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(true);
        else
            gameObject.SetActive(true);
    }


    public void SetDeckState(bool isInDeck)
    {
        if (deckHighlight != null)
            deckHighlight.SetActive(isInDeck && !_isLocked);
    }


    public void OnPointerClick(PointerEventData eventData)
    {
        if (_deckManager == null)
            return;

        if (_isLocked)
            return;

        if (_isDeckSlot)
        {
            if (_definition != null)
            {
                _deckManager.RemoveFromDeck(_definition);
            }
            return;
        }

        if (_definition != null)
        {
            _deckManager.ToggleUnit(_definition);
        }
    }
}
