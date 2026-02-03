using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Controls the Unit Details popup content (data + tabs).
/// All open/close animations are handled by PopupAnimator.
/// </summary>
public class UnitDetailsPopupController : MonoBehaviour
{
    public static UnitDetailsPopupController Instance;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundCloseButton;

    [Header("Animator (handles open/close)")]
    [SerializeField] private PopupAnimator popupAnimator;

    [Header("Unit UI")]
    [SerializeField] private Image icon;
    [SerializeField] private Animator iconAnimator;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text atkSpeedText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text speedText;
    [SerializeField] private TMP_Text targetPriorityText;
    [SerializeField] private TMP_Text SoulsCostText;
    [SerializeField] private Image rarityTagImage;
    [SerializeField] private Image unitClassImage;

    [Header("Rarity Sprites")]
    [SerializeField] private Sprite commonRaritySprite;
    [SerializeField] private Sprite rareRaritySprite;
    [SerializeField] private Sprite epicRaritySprite;
    [SerializeField] private Sprite legendaryRaritySprite;

    [Header("Unit Classes Sprites")]
    [SerializeField] private Sprite meleeClassSprite;
    [SerializeField] private Sprite rangedClassSprite;
    [SerializeField] private Sprite mageClassSprite;
    [SerializeField] private Sprite supportClassSprite;

    [Header("Tabs")]
    [SerializeField] private GameObject statsTab;
    [SerializeField] private GameObject partsTab;
    [SerializeField] private GameObject upgradesTab;
    [SerializeField] private Button statsTabButton;
    [SerializeField] private Button partsTabButton;
    [SerializeField] private Button upgradesTabButton;

    [Header("Parts Tab Controller")]
    [SerializeField] private UnitPartsTabController partsTabController;

    [Header("Parts Tab Controller")]
    [SerializeField] private UpgradeTreeUIController upgradesTabController;

    private UnitDefinition _unit;
    private UnitsDeckManager _deckManager;
    private RectTransform _lastOpenedCardRect;

    private void Awake()
    {
        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (backgroundCloseButton != null)
            backgroundCloseButton.onClick.AddListener(Close);

        // Keep root disabled initially (PopupAnimator also disables it in Awake, but this is safe)
        if (root != null)
            root.SetActive(false);
    }

    /// <summary>
    /// Opens the popup and fills the UI with unit data.
    /// The popup animation/positioning is handled by PopupAnimator.
    /// </summary>
    public void OpenFromCard(UnitDefinition unit, bool isDeckSlot, UnitsDeckManager deckManager, RectTransform cardRect)
    {
        if (unit == null)
        {
            Debug.LogWarning("UnitDetailsPopupController.OpenFromCard: unit is null");
            return;
        }

        _unit = unit;
        _deckManager = deckManager;
        _lastOpenedCardRect = cardRect;

        // Ensure popup is visible before filling (so SetNativeSize etc. behave consistently)
        if (root != null)
            root.SetActive(true);

        // Default tab on open
        OpenStatsTab();

        // Fill UI content
        FillData();

        // Delegate open animation to PopupAnimator
        if (popupAnimator != null && cardRect != null)
        {
            // If you want the source card to be hidden while popup is open, set hideButton=true
            popupAnimator.OpenFromRect(cardRect, hideButton: false);
        }
        else
        {
            Debug.LogWarning("UnitDetailsPopupController: popupAnimator or cardRect is missing. Opening without animation.");
        }
    }

    /// <summary>
    /// Closes the popup using PopupAnimator.
    /// </summary>
    public void Close()
    {
        if (root == null || !root.activeSelf)
            return;

        // Reset to default tab so next open always starts clean
        OpenStatsTab();

        if (popupAnimator != null && _lastOpenedCardRect != null && _lastOpenedCardRect.gameObject.activeInHierarchy)
        {
            popupAnimator.Close();
        }
        else
        {
            // Fallback: instant close
            root.SetActive(false);
        }
    }

    // =========================
    // DATA
    // =========================

    private void FillData()
    {
        if (_unit == null)
            return;

        if (partsTabController != null)
            partsTabController.Show(_unit);

        FillIconImageOrAnimator();

        if (nameText != null) nameText.text = _unit.displayName;
        if (descriptionText != null) descriptionText.text = _unit.description;

        if (hpText != null) hpText.text = _unit.maxHealth.ToString();
        if (dmgText != null) dmgText.text = _unit.damage.ToString();
        if (atkSpeedText != null) atkSpeedText.text = _unit.attackCooldown.ToString();
        if (rangeText != null) rangeText.text = _unit.attackRange.ToString();
        if (speedText != null) speedText.text = _unit.moveSpeed.ToString();

        if (targetPriorityText != null) targetPriorityText.text = _unit.targetPriorityClass.ToString();
        if (SoulsCostText != null) SoulsCostText.text = _unit.soulCost.ToString();

        if (rarityTagImage != null) rarityTagImage.sprite = GetRaritySpriteByType(_unit.rarity);
        if (unitClassImage != null) unitClassImage.sprite = GetUnitClassSpriteByType(_unit.unitClass);
    }

    private void FillIconImageOrAnimator()
    {
        if (icon == null || _unit == null)
            return;

        icon.sprite = _unit.icon;

        if (iconAnimator != null)
        {
            if (_unit.animatorController)
            {
                iconAnimator.enabled = true;
                iconAnimator.runtimeAnimatorController = _unit.animatorController;
            }
            else
            {
                iconAnimator.enabled = false;
                iconAnimator.runtimeAnimatorController = null;
            }
        }

        icon.SetNativeSize();
    }

    private Sprite GetRaritySpriteByType(UnitRarity type)
    {
        switch (type)
        {
            case UnitRarity.Common: return commonRaritySprite;
            case UnitRarity.Rare: return rareRaritySprite;
            case UnitRarity.Epic: return epicRaritySprite;
            case UnitRarity.Legendary: return legendaryRaritySprite;
            default: return commonRaritySprite;
        }
    }

    private Sprite GetUnitClassSpriteByType(UnitClass type)
    {
        switch (type)
        {
            case UnitClass.Melee: return meleeClassSprite;
            case UnitClass.Ranged: return rangedClassSprite;
            case UnitClass.Mage: return mageClassSprite;
            case UnitClass.Support: return supportClassSprite;
            default: return meleeClassSprite;
        }
    }

    // =========================
    // TABS
    // =========================

    public void OpenStatsTab()
    {
        if (statsTab != null) statsTab.SetActive(true);
        if (upgradesTab != null) upgradesTab.SetActive(false);
        if (partsTab != null) partsTab.SetActive(false);

        if (statsTabButton != null) statsTabButton.interactable = false;
        if (upgradesTabButton != null) upgradesTabButton.interactable = true;
        if (partsTabButton != null) partsTabButton.interactable = true;
    }

    public void OpenPartsTab()
    {
        if (partsTab != null) partsTab.SetActive(true);
        if (statsTab != null) statsTab.SetActive(false);
        if (upgradesTab != null) upgradesTab.SetActive(false);

        if (partsTabButton != null) partsTabButton.interactable = false;
        if (statsTabButton != null) statsTabButton.interactable = true;
        if (upgradesTabButton != null) upgradesTabButton.interactable = true;

        if (partsTabController != null && _unit != null)
            partsTabController.Refresh();
    }

    public void OpenUpgradesTab()
    {
        if (statsTab != null) statsTab.SetActive(false);
        if (partsTab != null) partsTab.SetActive(false);
        if (upgradesTab != null) upgradesTab.SetActive(true);

        if (upgradesTabButton != null) upgradesTabButton.interactable = false;
        if (partsTabButton != null) partsTabButton.interactable = true;
        if (statsTabButton != null) statsTabButton.interactable = true;

        upgradesTabController.SetUnit(_unit);
    }
}
