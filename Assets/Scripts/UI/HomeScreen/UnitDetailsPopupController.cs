using UnityEngine;
using UnityEngine.UI;
using TMPro;
using System;

public class UnitDetailsPopupController : MonoBehaviour
{
    public static UnitDetailsPopupController Instance;

    [Header("UI Refs")]
    public GameObject root;
    public Image iconImage;
    public TMP_Text unitNameText;
    public TMP_Text levelText;
    public TMP_Text rarityText;

    public TMP_Text hpText;
    public TMP_Text damageText;
    public TMP_Text attackSpeedText;
    public TMP_Text rangeText;

    public TMP_Text inDeckText;

    [Header("Buttons")]
    public Button toggleDeckButton;
    public TMP_Text toggleDeckButtonText;
    public Button closeButton;

    private UnitDefinition currentUnit;

    private void Awake()
    {
        Instance = this;
        root.SetActive(false);
    }

    private void Start()
    {
        closeButton.onClick.AddListener(Hide);
        toggleDeckButton.onClick.AddListener(OnToggleDeck);
    }

    public void Show(UnitDefinition unit)
    {
        if (unit == null) return;

        currentUnit = unit;

        root.SetActive(true);

        iconImage.sprite = unit.icon;
        iconImage.SetNativeSize();

        unitNameText.text = unit.displayName;
        rarityText.text = unit.rarity.ToString();

        hpText.text = unit.maxHealth.ToString();
        damageText.text = unit.damage.ToString();
        attackSpeedText.text = unit.attackCooldown.ToString();
        rangeText.text = unit.attackRange.ToString();

        //int level = PlayerUnitsProgress.Instance.GetUnitLevel(unit.id);
        //levelText.text = "Lv. " + level;

        RefreshDeckState();
    }

    private void RefreshDeckState()
    {
        bool inDeck = UnitsDeckManager.Instance.IsInDeck(currentUnit);

        inDeckText.text = inDeck ? "In Deck" : "Not In Deck";
        toggleDeckButtonText.text = inDeck ? "Remove From Deck" : "Add To Deck";
    }

    public void OnToggleDeck()
    {
        UnitsDeckManager.Instance.ToggleUnit(currentUnit);
        RefreshDeckState();
    }

    public void Hide()
    {
        root.SetActive(false);
        currentUnit = null;
    }

    public void OnBackgroundClick()
    {
        Hide();
    }

    internal void Open(UnitDefinition definition, bool isInDeck, UnitsDeckManager deckManager)
    {
        throw new NotImplementedException();
    }
}
