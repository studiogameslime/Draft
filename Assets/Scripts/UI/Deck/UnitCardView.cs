using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class UnitCardView : MonoBehaviour, IPointerClickHandler
{
    [Header("UI")]
    [SerializeField] private Image iconImage;
    [SerializeField] private Animator iconAnimator;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private GameObject emptySlotVisual;

    [Header("Quick Actions (inside card)")]
    [SerializeField] private GameObject quickActionsPanel;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button equipButton;

    [Header("Rarity Colors")]
    [SerializeField] private Color CommonRarityColor = Color.white;
    [SerializeField] private Color RareRarityColor = Color.white;
    [SerializeField] private Color EpicRarityColor = Color.white;
    [SerializeField] private Color LegendaryRarityColor = Color.white;

    [Header("Lock State")]
    [SerializeField] private Color lockedColor = Color.gray;

    private UnitDefinition _definition;
    private UnitsDeckManager _deckManager;
    private bool _isDeckSlot;
    private bool _isLocked;

    public RectTransform RectTransform { get; private set; }
    private Vector2 _originalSize;

    // replace-mode visual
    private Coroutine _wiggleRoutine;
    private Image cardImage;

    public UnitDefinition Definition => _definition;
    private UnitsCollectionManager unitsCollectionManager;

    private UnitUnlockState _unlockState;


    private void Awake()
    {
        cardImage = GetComponent<Image>();
        RectTransform = GetComponent<RectTransform>();
        _originalSize = RectTransform.sizeDelta;

        if (quickActionsPanel != null)
            quickActionsPanel.SetActive(false);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeButtonPressed);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonPressed);

        if (unitsCollectionManager == null)
        {
            unitsCollectionManager = FindAnyObjectByType<UnitsCollectionManager>();
        }
    }

    // -------------------------------------------------
    // SETUP
    // -------------------------------------------------
    public void Setup(
        UnitDefinition def,
        UnitsDeckManager deckManager,
        UnitUnlockState unlockState,
        bool isInDeck,
        bool isDeckSlot)
    {
        _definition = def;
        _deckManager = deckManager;
        _isDeckSlot = isDeckSlot;
        _unlockState = unlockState;


        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(false);

        if (def != null)
        {
            if (iconImage != null)
                iconImage.sprite = def.icon;
            if (nameText != null)
                nameText.text = def.displayName;

            FillIconImageOrAnimator();
        }

        switch (_unlockState)
        {
            case UnitUnlockState.Undiscovered:
                iconImage.color = Color.black;
                cardImage.color = lockedColor;
                equipButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(false);
                break;

            case UnitUnlockState.Discovered:
                iconImage.color = Color.white;
                cardImage.color = lockedColor;
                equipButton.gameObject.SetActive(false);
                upgradeButton.gameObject.SetActive(true);
                break;

            case UnitUnlockState.Unlocked:
                iconImage.color = Color.white;
                SetRarityStyle(def);
                equipButton.gameObject.SetActive(!_deckManager.IsInDeck(def));
                upgradeButton.gameObject.SetActive(true);
                break;
        }


        if (quickActionsPanel != null)
            quickActionsPanel.SetActive(false);

        _originalSize = RectTransform.sizeDelta;
        gameObject.SetActive(true);
    }

    public void SetupEmptySlot()
    {
        _definition = null;
        _isDeckSlot = true;
        _isLocked = false;

        if (iconImage != null)
            iconImage.transform.parent.gameObject.SetActive(false);

        if (nameText != null)
            nameText.gameObject.SetActive(false);

        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(true);

        if (quickActionsPanel != null)
            quickActionsPanel.SetActive(false);

        if (cardImage != null)
            cardImage.color = lockedColor;

        _originalSize = RectTransform.sizeDelta;
        gameObject.SetActive(true);
    }

    // -------------------------------------------------
    // RARITY / STYLE
    // -------------------------------------------------
    private void SetRarityStyle(UnitDefinition def)
    {
        if (def == null || cardImage == null) return;

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
        }
    }

    private void FillIconImageOrAnimator()
    {
        if (_definition == null || iconImage == null) return;

        iconImage.sprite = _definition.icon;

        if (_definition.animatorController)
        {
            iconAnimator.enabled = true;
            iconAnimator.runtimeAnimatorController = _definition.animatorController;
        }
        else
        {
            iconAnimator.enabled = false;
            iconAnimator.runtimeAnimatorController = null;
        }

        iconImage.SetNativeSize();
    }

    // -------------------------------------------------
    // CLICK & QUICK ACTIONS
    // -------------------------------------------------
    public void OnPointerClick(PointerEventData eventData)
    {
        if (_definition == null)
            return;

        if (_isLocked)
            return;

        if (_isDeckSlot && UnitsDeckManager.Instance != null &&
            UnitsDeckManager.Instance.IsReplaceModeActive)
        {
            UnitsDeckManager.Instance.ReplaceWithDeckCard(_definition);
            unitsCollectionManager.RebuildCollection();

            return;
        }

        if (_isDeckSlot)
            return;

        if (UnitsGridController.Instance != null)
        {
            UnitsGridController.Instance.OnCardClicked(this);
        }
    }

    public void Expand(float extraHeight)
    {
        float newHeight = _originalSize.y + extraHeight;
        RectTransform.sizeDelta = new Vector2(_originalSize.x, newHeight);
    }

    public void Collapse()
    {
        RectTransform.sizeDelta = _originalSize;
    }

    public void ShowQuickActions(bool show)
    {
        if (quickActionsPanel != null)
            quickActionsPanel.SetActive(show);
    }

    private void OnUpgradeButtonPressed()
    {
        if (_definition == null || _deckManager == null || _isLocked)
            return;

        UnitDetailsPopupController.Instance.OpenFromCard(
            _definition,
            isDeckSlot: _isDeckSlot,
            deckManager: _deckManager,
            GetComponent<RectTransform>()
        );
    }

    private void OnEquipButtonPressed()
    {
        if (_definition == null || _deckManager == null || _isLocked)
            return;

        if (_deckManager.IsInDeck(_definition))
            return;

        if (_deckManager.CurrentDeck.Count < UnitsDeckManager.MaxDeckSize)
        {
            _deckManager.ToggleUnit(_definition);
            unitsCollectionManager.RebuildCollection();
            UnitsGridController.Instance?.CollapseCurrent();
            return;
        }

        UnitsGridController.Instance?.CollapseCurrent();
        _deckManager.StartReplaceMode(_definition);
    }

    // --------- replace-mode visual ("dance") ---------
    public void SetReplaceModeVisual(bool active)
    {
        if (!_isDeckSlot)
            return;

        if (active)
        {
            if (_wiggleRoutine == null)
                _wiggleRoutine = StartCoroutine(Wiggle());
        }
        else
        {
            if (_wiggleRoutine != null)
            {
                StopCoroutine(_wiggleRoutine);
                _wiggleRoutine = null;
            }
            transform.localScale = Vector3.one;
        }
    }

    private IEnumerator Wiggle()
    {
        while (true)
        {
            float t = Mathf.Sin(Time.time * 6f) * 0.05f;
            float s = 1f + t;
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }
}
