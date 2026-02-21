using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleBottomPanelController : MonoBehaviour
{

    public static BattleBottomPanelController Instance;

    [Header("Single Container (already has HorizontalLayoutGroup)")]
    [SerializeField] private RectTransform contentParent;

    [Header("Deck")]
    [SerializeField] private UnitSpawnButton deckButtonPrefab;

    [Header("Upgrades")]
    [SerializeField] private BattleUpgradeOptionView upgradeOptionPrefab;

    [Header("SellUnit")]
    [SerializeField] private GameObject sellUnitButton;

    [Header("PlaceholderText")]
    [SerializeField] private TMP_Text placeholderText;
    [SerializeField] public string defaultPlaceholder;
    [SerializeField] public string fullUpgradePlaceholder;

    [Header("Cell Bonus")]
    [SerializeField] private GameObject cellBonusRoot;
    [SerializeField] private TMP_Text cellBonusText;


    private void Awake()
    {
        Instance = this;
        if (sellUnitButton != null)
            sellUnitButton.SetActive(false);
    }

    public void Clear()
    {
        if (sellUnitButton != null)
            sellUnitButton.SetActive(false);

        if (contentParent == null) return;

        for (int i = contentParent.childCount - 1; i >= 0; i--)
            Destroy(contentParent.GetChild(i).gameObject);
    }

    // =========================
    // DECK
    // =========================
    public void ShowDeck(List<UnitDefinition> deck)
    {
        if (contentParent == null || deckButtonPrefab == null)
            return;

        Clear();

        if (deck != null)
        {
            foreach (var unit in deck)
            {
                if (unit == null) continue;
                var btn = Instantiate(deckButtonPrefab, contentParent);
                btn.Init(unit); 
            }
        }

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
    }


    // =========================
    // UPGRADES (2 options)
    // =========================
    public void ShowUpgrades(UnitSpawner spawner, UnitDefinition unit, int tierToShow)
    {
        if (contentParent == null || upgradeOptionPrefab == null) return;
        if (unit == null) return;

        Clear();

        var progress = UnitUpgradeProgressService.GetOrCreateUnitProgress(unit.id);

        var leftNode = BattleUpgradeLogic.FindNode(unit, tierToShow, UpgradeLane.Left);
        var rightNode = BattleUpgradeLogic.FindNode(unit, tierToShow, UpgradeLane.Right);

        if (leftNode != null && rightNode != null)
        {
            CreateUpgradeCard(spawner, unit, progress, leftNode);
            CreateUpgradeCard(spawner, unit, progress, rightNode);

            LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
            ShowSellButton(spawner);
            HidePlaceholderText();
        }
        else
        {
            ShowSellButton(spawner);
            ShowPlaceholderText(fullUpgradePlaceholder);
            Debug.Log("unit fully upgraded OR need to define upgrades to this unit!");
        }


    }

    private void CreateUpgradeCard(UnitSpawner spawner, UnitDefinition unit, UnitProgressData progress, UnitUpgradeNodeDefinition node)
    {

        var view = Instantiate(upgradeOptionPrefab, contentParent);

        bool exists = node != null;
        bool metaUnlocked = exists &&
                            progress != null &&
                            progress.unlockedUpgradeNodeIds != null &&
                            progress.unlockedUpgradeNodeIds.Contains(node.nodeId);

        view.Bind(
            node,
            isLocked: !metaUnlocked,
            onClick: () =>
            {
                if (!metaUnlocked || node == null || spawner == null) return;

                bool ok = spawner.TryBuyUpgrade(node);
                if (!ok) return;

                var state = spawner.GetComponent<UnitSpawnerBattleUpgradeState>();


                var upgradesUI = spawner.GetComponentInChildren<BattleUpgradesManager>(true);
                if (upgradesUI != null)
                    upgradesUI.AddUpgradeIcon(node);

                ShowUpgrades(spawner, unit, state.currentTier);
            }
        );
    }

    public void ShowSellButton(UnitSpawner spawner)
    {
        sellUnitButton.SetActive(spawner != null);
        sellUnitButton.GetComponent<UnitSellButton>().Init(spawner);
    }
    public void HideSellButton()
    {
        if (sellUnitButton != null)
            sellUnitButton.SetActive(false);
    }
    public void ShowPlaceholderText(string placeholder)
    {
        placeholderText.text = placeholder;
        placeholderText.gameObject.SetActive(true);
    }
    public void HidePlaceholderText()
    {
        placeholderText.gameObject.SetActive(false);
    }

    public void ShowCellBonus(DropAreaCell cell)
    {
        if (cellBonusRoot == null || cellBonusText == null) return;

        // Hide if the cell is null or doesn't have a special bonus/penalty
        if (cell == null || !cell.IsSpecial)
        {
            cellBonusRoot.SetActive(false);
            return;
        }

        cellBonusRoot.SetActive(true);

        // Determine the stat name using a switch statement for future scalability
        string statName = "";
        switch (cell.bonusType)
        {
            case CellBonusType.AttackPercent:
                statName = "Attack";
                break;
            case CellBonusType.HpPercent:
                statName = "HP";
                break;
            // Add new bonus types here in the future:
            // case CellBonusType.SpeedPercent:
            //     statName = "Speed";
            //     break;
            default:
                statName = "Stat";
                break;
        }

        // Add a plus sign for positive values (negative values naturally include a minus sign)
        string sign = cell.percentValue > 0 ? "+" : "";

        cellBonusText.text = $"{sign}{cell.percentValue}% {statName}";

        // Set the text color to green for bonus, red for penalty
        if (cell.percentValue > 0)
        {
            cellBonusText.color = Color.green;
        }
        else
        {
            cellBonusText.color = Color.red;
        }
    }

    public void HideCellBonus()
    {
        if (cellBonusRoot != null)
        {
            cellBonusRoot.SetActive(false);
        }
    }
}
