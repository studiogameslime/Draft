using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using TMPro;
using System;

public class UnitCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private TMP_Text nameText;
    private Image cardImage;

    [Header("States")]
    [SerializeField] private GameObject lockedOverlay;
    [SerializeField] private GameObject deckHighlight;
    [SerializeField] private GameObject emptySlotVisual;

    private UnitDefinition _definition;
    private UnitsDeckManager _deckManager;
    private bool _isLocked;
    private bool _isDeckSlot;

    [SerializeField] Color CommonRarityColor = Color.white;
    [SerializeField] Color RareRarityColor = Color.white;
    [SerializeField] Color EpicRarityColor = Color.white;
    [SerializeField] Color LegendaryRarityColor = Color.white;

    public UnitDefinition Definition => _definition;

    private void Awake()
    {
        cardImage = GetComponent<Image>();
    }



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

        SetRarityStyle(def);
    }

    private void SetRarityStyle(UnitDefinition def)
    {
        switch (def.rarity)
        {
            case UnitRarity.Common:
                cardImage.color = CommonRarityColor;
                break;
            case UnitRarity.Rare:
                cardImage.color = RareRarityColor;
                break;
            case UnitRarity.Epic:
                cardImage.color = EpicRarityColor;
                break;
            case UnitRarity.Legendary:
                cardImage.color = LegendaryRarityColor;
                break;
            default:
                break;
        }
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
