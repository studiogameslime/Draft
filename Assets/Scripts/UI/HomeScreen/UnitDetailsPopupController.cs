using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitDetailsPopupController : MonoBehaviour
{
    public static UnitDetailsPopupController Instance;

    [Header("Root")]
    [SerializeField] private GameObject root;
    [SerializeField] private Button closeButton;
    [SerializeField] private Button backgroundCloseButton;

    [Header("Unit UI")]
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text nameText;
    [SerializeField] private TMP_Text rarityText;
    [SerializeField] private TMP_Text hpText;
    [SerializeField] private TMP_Text dmgText;
    [SerializeField] private TMP_Text atkSpeedText;
    [SerializeField] private TMP_Text rangeText;
    [SerializeField] private TMP_Text levelText;

    [Header("Main Button")]
    [SerializeField] private Button toggleDeckButton;
    [SerializeField] private TMP_Text toggleDeckButtonText;

    [Header("Replace Panel")]
    [SerializeField] private GameObject replacePanel;
    [SerializeField] private Transform replaceButtonsParent;
    [SerializeField] private Button replaceButtonPrefab;

    private UnitDefinition _unit;
    private UnitsDeckManager _deckManager;

    void Awake()
    {
        Instance = this;
        root.SetActive(false);
        replacePanel.SetActive(false);

        closeButton.onClick.AddListener(Close);
        backgroundCloseButton.onClick.AddListener(Close);
    }

    public void Open(UnitDefinition unit, bool isDeckSlot, UnitsDeckManager deckManager)
    {
        _unit = unit;
        _deckManager = deckManager;

        FillData();
        SetupButtons();

        replacePanel.SetActive(false);
        root.SetActive(true);
    }

    public void Close()
    {
        root.SetActive(false);
    }

    private void FillData()
    {
        icon.sprite = _unit.icon;
        nameText.text = _unit.displayName;
        rarityText.text = _unit.rarity.ToString();
        hpText.text = _unit.maxHealth.ToString();
        dmgText.text = _unit.damage.ToString();
        atkSpeedText.text = _unit.attackCooldown.ToString();
        rangeText.text = _unit.attackRange.ToString();
        levelText.text = "Lvl 1";
    }

    private void SetupButtons()
    {
        toggleDeckButton.onClick.RemoveAllListeners();

        if (!_deckManager.IsInDeck(_unit))
        {
            toggleDeckButtonText.text = "Add to Deck";
            toggleDeckButton.onClick.AddListener(TryAddUnitToDeck);
        }
        else
        {
            toggleDeckButtonText.text = "Remove from Deck";
            toggleDeckButton.onClick.AddListener(() =>
            {
                _deckManager.RemoveFromDeck(_unit);
                Close();
            });
        }
    }

    private void TryAddUnitToDeck()
    {
        if (_deckManager.CurrentDeck.Count < UnitsDeckManager.MaxDeckSize)
        {
            _deckManager.ToggleUnit(_unit);
            Close();
            return;
        }

        BuildReplacePanel();
    }

    private void BuildReplacePanel()
    {
        replacePanel.SetActive(true);

        foreach (Transform child in replaceButtonsParent)
            Destroy(child.gameObject);

        foreach (var oldUnit in _deckManager.CurrentDeck)
        {
            var btn = Instantiate(replaceButtonPrefab, replaceButtonsParent);
            btn.GetComponentInChildren<TMP_Text>().text = oldUnit.displayName;

            btn.gameObject.AddComponent<ReplaceDeckButton>()
                .Setup(_unit, oldUnit, _deckManager);
        }
    }
}
