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
    [SerializeField] private TMP_Text spawnTimeText;
    [SerializeField] private TMP_Text capcityText;
    [SerializeField] private TMP_Text SoulsCostText;
    [SerializeField] private Image rarityTagImage;
    [SerializeField] private Image unitClassImage;
    [SerializeField] private TMP_Text addHpText;
    [SerializeField] private TMP_Text addDmgText;
    [SerializeField] private TMP_Text decreaseSpawnTimeText;
    [SerializeField] private TMP_Text damageHealTitle;

    [Header("Leveling UI")]
    [SerializeField] private TMP_Text levelText;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private TMP_Text upgradeCostText;
    [HideInInspector] private Image upgradeCostIcon;


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

    [SerializeField] private Sprite selectedTab;
    [SerializeField] private Sprite unselectedTab;

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

        if (upgradeButton != null)
        {
            upgradeButton.onClick.RemoveListener(OnUpgradeClicked);
            upgradeButton.onClick.AddListener(OnUpgradeClicked);
        }

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

        // ---------- LEVEL DATA ----------
        int lvl = UnitLevelingService.GetUnitLevel(_unit.id);
        int maxLvl = UnitLevelingService.MaxLevel;

        float mulNow = UnitLevelingService.GetHpDamageMultiplier(lvl);
        int hpNow = Mathf.FloorToInt(_unit.maxHealth * mulNow);
        int dmgNow = Mathf.FloorToInt(_unit.damage * mulNow);

        //float currentSpawnTime = _unit.spawnTime * UnitLevelingService.GetSpawnTimeMultiplier(lvl);
        //float nextSpawnTime = currentSpawnTime;
        //if (lvl < maxLvl)
        //{
        //    nextSpawnTime = _unit.spawnTime *
        //                    UnitLevelingService.GetSpawnTimeMultiplier(lvl + 1);
        //}
        //float spawnDecrease = (lvl < maxLvl) ? currentSpawnTime - nextSpawnTime : 0f;


        // ---------- STATS TEXT ----------
        if (damageHealTitle != null) 
        {
            damageHealTitle.text = _unit.displayName == "Fairy" ? "Heal" : "Damage";
        }

        if (hpText != null) hpText.text = hpNow.ToString();
        if (dmgText != null) dmgText.text = dmgNow.ToString();


        if (spawnTimeText != null) spawnTimeText.text = _unit.spawnTime.ToString();

        //if (decreaseSpawnTimeText != null)
        //{
        //    decreaseSpawnTimeText.text = (lvl < maxLvl)
        //        ? $"-{spawnDecrease.ToString("0.#")}"
        //        : "-";
        //}

            //if (spawnTimeText != null)
            //{
            //    spawnTimeText.text = currentSpawnTime.ToString("0.#");
            //}


        if (atkSpeedText != null) atkSpeedText.text = _unit.attackCooldown.ToString();

        var label = UILabels.GetRangeLabel(_unit.attackRange);
        rangeText.text = UILabels.RangeToDisplay(label);

        var speedCategory = UILabels.GetSpeedLabel(_unit.moveSpeed);
        speedText.text = UILabels.SpeedToDisplay(speedCategory);


        if (targetPriorityText != null) targetPriorityText.text = _unit.targetPriorityClass.ToString();
        if (SoulsCostText != null) SoulsCostText.text = _unit.soulCost.ToString();

        if (capcityText != null) capcityText.text = _unit.baseCapacity.ToString();


        if (rarityTagImage != null) rarityTagImage.sprite = GetRaritySpriteByType(_unit.rarity);
        if (unitClassImage != null) unitClassImage.sprite = GetUnitClassSpriteByType(_unit.unitClass);

        // ---------- LEVEL UI ----------
        if (levelText != null)
            levelText.text = $"Level {lvl}/{maxLvl}";

        // ---------- COST + BUTTON STATE ----------
        bool atMax = lvl >= maxLvl;

        if (!atMax)
        {
            float mulNext = UnitLevelingService.GetHpDamageMultiplier(lvl + 1);
            int hpNext = Mathf.FloorToInt(_unit.maxHealth * mulNext);
            int dmgNext = Mathf.FloorToInt(_unit.damage * mulNext);

            int hpAdd = hpNext - hpNow;
            int dmgAdd = dmgNext - dmgNow;

            if (addHpText != null)
            {
                addHpText.gameObject.SetActive(true);
                addHpText.text = $"+{hpAdd}";
            }
            if (addDmgText != null)
            {
                addDmgText.gameObject.SetActive(true);
                addDmgText.text = $"+{dmgAdd}";
            }
        }
        else
        {
            if (addHpText != null) addHpText.gameObject.SetActive(false);
            if (addDmgText != null) addDmgText.gameObject.SetActive(false);
        }

        int cost = atMax ? 0 : UnitLevelingService.GetUpgradeCost(lvl);

        if (upgradeCostText != null)
            upgradeCostText.text = atMax ? "MAX" : cost.ToString();

        if (upgradeCostIcon != null) upgradeCostIcon.sprite = StyleManager.instance.scrollSprite;

        int ownedScrolls = 0;
        if (PlayerCurrencyWallet.Instance != null)
            ownedScrolls = PlayerCurrencyWallet.Instance.Scrolls;

        bool inBattle = (BattleManager.instance != null && BattleManager.instance.IsBattleRunning);
        bool canAfford = ownedScrolls >= cost;

        if (upgradeButton != null)
            upgradeButton.interactable = (!inBattle) && (!atMax) && canAfford;
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

        // FORCE RECT RESET
        RectTransform rt = icon.rectTransform;
        rt.anchorMin = Vector2.zero;
        rt.anchorMax = Vector2.one;
        rt.offsetMin = Vector2.zero;
        rt.offsetMax = Vector2.zero;
        //icon.SetNativeSize();
    }

    private void OnUpgradeClicked()
    {
        if (_unit == null) return;

        bool ok = UnitLevelingService.TryUpgradeUnit(_unit.id);
        if (!ok) return;

        FillData();

        int unitLvl = UnitLevelingService.GetUnitLevel(_unit.id);
        PlayerXPManager.Instance.AddXP(unitLvl * 10);

        if (upgradesTab != null && upgradesTab.activeSelf && upgradesTabController != null)
            upgradesTabController.SetUnit(_unit);
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

        if (statsTabButton != null)
        {
            statsTabButton.image.sprite = selectedTab;
            //statsTabButton.interactable = false;
        }
        if (upgradesTabButton != null)
        {
            upgradesTabButton.image.sprite = unselectedTab;
            //upgradesTabButton.interactable = true;
        }
        if (partsTabButton != null)
        {
            partsTabButton.image.sprite = unselectedTab;
            //partsTabButton.interactable = true;
        }
    }

    public void OpenPartsTab()
    {
        if (partsTab != null) partsTab.SetActive(true);
        if (statsTab != null) statsTab.SetActive(false);
        if (upgradesTab != null) upgradesTab.SetActive(false);

        if (partsTabButton != null)
        {
            partsTabButton.image.sprite = selectedTab;
            //partsTabButton.interactable = false;
        }
        if (statsTabButton != null)
        {
            statsTabButton.image.sprite = unselectedTab;
            //statsTabButton.interactable = true;
        }
        if (upgradesTabButton != null)
        {
            upgradesTabButton.image.sprite = unselectedTab;
            //upgradesTabButton.interactable = true;
        }

        if (partsTabController != null && _unit != null)
            partsTabController.Refresh();
    }

    public void OpenUpgradesTab()
    {
        if (statsTab != null) statsTab.SetActive(false);
        if (partsTab != null) partsTab.SetActive(false);
        if (upgradesTab != null) upgradesTab.SetActive(true);

        if (upgradesTabButton != null)
        {
            upgradesTabButton.image.sprite = selectedTab;
            //upgradesTabButton.interactable = false;
        }
        if (partsTabButton != null)
        {
            partsTabButton.image.sprite = unselectedTab;
            //partsTabButton.interactable = true;
        }
        if (statsTabButton != null)
        {
            statsTabButton.image.sprite = unselectedTab;
            //statsTabButton.interactable = true;
        }

        upgradesTabController.SetUnit(_unit);
    }

    private void OnEnable()
    {
        if (PlayerCurrencyWallet.Instance != null)
            PlayerCurrencyWallet.Instance.OnScrollsChanged += OnScrollsChanged;
    }

    private void OnDisable()
    {
        if (PlayerCurrencyWallet.Instance != null)
            PlayerCurrencyWallet.Instance.OnScrollsChanged -= OnScrollsChanged;
    }

    private void OnScrollsChanged(int _)
    {
        if (root != null && root.activeSelf)
            FillData();
    }

}
