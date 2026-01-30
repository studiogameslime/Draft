using System.Collections.Generic;
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

    [Header("FooterText")]
    [SerializeField] private GameObject placeholderText;



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
    public void ShowDeck(List<UnitDefinition> deck, UnitSelectionUI owner)
    {
        if (contentParent == null || deckButtonPrefab == null) return;

        Clear();

        if (deck != null)
        {
            foreach (var unit in deck)
            {
                if (unit == null) continue;
                var btn = Instantiate(deckButtonPrefab, contentParent);
                btn.Init(unit, owner);
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

        CreateUpgradeCard(spawner, unit, progress, leftNode);
        CreateUpgradeCard(spawner, unit, progress, rightNode);

        LayoutRebuilder.ForceRebuildLayoutImmediate(contentParent);
        ShowSellButton(spawner);
        HidePlaceholderText();


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
    public void ShowPlaceholderText()
    {
        placeholderText.SetActive(true);
    }
    public void HidePlaceholderText()
    {
        placeholderText.SetActive(false);
    }
}
