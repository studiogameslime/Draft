using System.Collections;
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

    [Header("Quick Actions (inside card)")]
    [SerializeField] private GameObject quickActionsPanel;
    [SerializeField] private Button upgradeButton;
    [SerializeField] private Button equipButton;

    private UnitDefinition _definition;
    private UnitsDeckManager _deckManager;
    private bool _isDeckSlot;

    public RectTransform RectTransform { get; private set; }
    private Vector2 _originalSize;

    // replace-mode visual
    private Coroutine _wiggleRoutine;

    private void Awake()
    {
        RectTransform = GetComponent<RectTransform>();
        _originalSize = RectTransform.sizeDelta;

        if (quickActionsPanel != null)
            quickActionsPanel.SetActive(false);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeButtonPressed);

        if (equipButton != null)
            equipButton.onClick.AddListener(OnEquipButtonPressed);
    }

    public void Setup(UnitDefinition def, UnitsDeckManager deckManager,
        bool isLocked, bool isInDeck, bool isDeckSlot)
    {
        _definition = def;
        _deckManager = deckManager;
        _isDeckSlot = isDeckSlot;

        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(false);

        if (def != null)
        {
            if (iconImage != null)
                iconImage.sprite = def.icon;

            if (nameText != null)
                nameText.text = def.displayName;
        }

        // hide Equip button in collection if already in deck
        if (!isDeckSlot && equipButton != null)
        {
            bool alreadyInDeck = _deckManager != null && _deckManager.IsInDeck(def);
            equipButton.gameObject.SetActive(!alreadyInDeck);
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

        if (iconImage != null)
            iconImage.sprite = null;

        if (nameText != null)
            nameText.text = "";

        if (emptySlotVisual != null)
            emptySlotVisual.SetActive(true);

        if (quickActionsPanel != null)
            quickActionsPanel.SetActive(false);

        _originalSize = RectTransform.sizeDelta;

        gameObject.SetActive(true);
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_definition == null)
            return;

        // click on deck card while replace-mode is active perform replace
        if (_isDeckSlot && UnitsDeckManager.Instance != null &&
            UnitsDeckManager.Instance.IsReplaceModeActive)
        {
            UnitsDeckManager.Instance.ReplaceWithDeckCard(_definition);
            return;
        }

        // deck cards (top row) – no other click behavior for now
        if (_isDeckSlot)
            return;

        // collection card: open/close quick actions
        if (UnitsGridController.Instance != null)
        {
            UnitsGridController.Instance.OnCardClicked(this);
        }
    }

    // called from UnitsGridController – increase ONLY height
    public void Expand(float extraHeight)
    {
        float newHeight = _originalSize.y + extraHeight;
        RectTransform.sizeDelta = new Vector2(_originalSize.x, newHeight);
    }

    // restore original size
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
        if (_definition == null || _deckManager == null)
            return;

        UnitDetailsPopupController.Instance.Open(
            _definition,
            isDeckSlot: _isDeckSlot,
            deckManager: _deckManager
        );
    }

    private void OnEquipButtonPressed()
    {
        if (_definition == null || _deckManager == null)
            return;

        // already in deck – nothing to do (button usually hidden anyway)
        if (_deckManager.IsInDeck(_definition))
            return;

        // deck not full add directly
        if (_deckManager.CurrentDeck.Count < UnitsDeckManager.MaxDeckSize)
        {
            _deckManager.ToggleUnit(_definition);
            UnitsGridController.Instance?.CollapseCurrent();
            return;
        }

        // deck full start replace mode: highlight deck cards and wait for click
        UnitsGridController.Instance?.CollapseCurrent();
        _deckManager.StartReplaceMode(_definition);
    }

    // --------- replace-mode visual ("dance") ---------
    public void SetReplaceModeVisual(bool active)
    {
        if (!_isDeckSlot)
            return; // we only animate deck cards

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
            float t = Mathf.Sin(Time.time * 6f) * 0.05f; // small 5% scale wiggle
            float s = 1f + t;
            transform.localScale = new Vector3(s, s, 1f);
            yield return null;
        }
    }
}
