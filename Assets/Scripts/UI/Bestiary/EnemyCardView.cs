using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.EventSystems;

public class EnemyCardView : MonoBehaviour, IPointerClickHandler
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

    [SerializeField] private GameObject inDeckCheckmark;

    [Header("Enemy Extras")]
    [SerializeField] private TMP_Text killCountText;

    private UnitDefinition _definition;
    private bool _discovered;
    private EnemyBestiaryScreen _screen;
    private Image _cardImage;

    public UnitDefinition Definition => _definition;
    public bool IsDiscovered => _discovered;
    public RectTransform RectTransform { get; private set; }

    private void Awake()
    {
        _cardImage = GetComponent<Image>();
        RectTransform = GetComponent<RectTransform>();
    }

    public void Setup(UnitDefinition def, bool discovered, int kills, EnemyBestiaryScreen screen)
    {
        _definition = def;
        _discovered = discovered;
        _screen = screen;

        // Hide unit-specific elements
        if (emptySlotVisual != null) emptySlotVisual.SetActive(false);
        if (quickActionsPanel != null) quickActionsPanel.SetActive(false);
        if (upgradeButton != null) upgradeButton.gameObject.SetActive(false);
        if (equipButton != null) equipButton.gameObject.SetActive(false);
        if (inDeckCheckmark != null) inDeckCheckmark.SetActive(false);

        if (def == null) return;

        // Name
        if (nameText != null)
            nameText.text = discovered ? def.displayName : "???";

        // Icon
        if (iconImage != null)
        {
            iconImage.sprite = def.icon;
            iconImage.preserveAspect = true;
            iconImage.color = discovered ? Color.white : Color.black;
        }

        // Animator
        if (iconAnimator != null)
        {
            if (discovered && def.animatorController != null)
            {
                iconAnimator.enabled = true;
                iconAnimator.runtimeAnimatorController = def.animatorController;
            }
            else
            {
                iconAnimator.enabled = false;
                iconAnimator.runtimeAnimatorController = null;
            }
        }

        // Kill count (reuse nameText area below or add a separate text — for now show in quick actions area)
        if (killCountText != null)
            killCountText.text = discovered ? $"Kills: {kills}" : "";

        // Card background color
        if (_cardImage != null)
            _cardImage.color = discovered ? Color.white : lockedColor;
    }

    public void OnPointerClick(PointerEventData eventData)
    {
        if (_screen != null)
            _screen.OnCardClicked(this);
    }
}
