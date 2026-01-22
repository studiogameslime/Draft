using UnityEngine;
using UnityEngine.EventSystems;

/// <summary>
/// Handles selecting a grid cell and showing the bottom deck UI for placement.
/// IMPORTANT: DeckUIController lives in another scene (CommonUI) which is loaded later,
/// so this script must "late-bind" to DeckUIController.
/// </summary>
public class BattleCellSelectionController : MonoBehaviour
{
    public static BattleCellSelectionController Instance;

    [Header("References")]
    [SerializeField] private BattleManager battleManager;
    [SerializeField] private UnitSpawner spawnerPrefab;

    // Deck UI is loaded from another scene, so it may be null at Start.
    private DeckUIController deckUI;

    // NEW: controller that renders deck/upgrades into the same container
    private BattleBottomPanelController bottomPanel;

    private DropAreaCell selectedCell;
    private Camera mainCam;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        mainCam = Camera.main;
    }

    private void Start()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        // Do not assume deckUI exists yet (CommonUI might not be loaded).
        TryResolveDeckUI();

        // If deckUI exists, hide it; otherwise it will be hidden once it is resolved.
        HideBottomPanel();
    }

    private void Update()
    {
        TryResolveDeckUI();

        if (!Application.isPlaying) return;
        if (battleManager != null && battleManager.IsBattleRunning) return;

        if (Input.GetMouseButtonDown(0))
        {
            // If clicking on UI (deck buttons etc.), do not clear selection
            if (EventSystem.current != null && EventSystem.current.IsPointerOverGameObject())
                return;

            if (mainCam == null) mainCam = Camera.main;
            if (mainCam == null) return;

            Vector3 world = mainCam.ScreenToWorldPoint(Input.mousePosition);
            RaycastHit2D hit = Physics2D.Raycast(world, Vector2.zero);

            if (!hit.collider || hit.collider.GetComponent<DropAreaCell>() == null)
                ClearSelection();
        }
    }

    /// <summary>
    /// Called from DropAreaCell.OnMouseDown().
    /// </summary>
    public void SelectCell(DropAreaCell cell)
    {
        if (cell == null)
            return;

        // Clicking the same cell again toggles selection off (hides deck).
        if (selectedCell == cell)
        {
            ClearSelection();
            return;
        }

        SetSelectedInternal(cell);

        if (battleManager != null && battleManager.IsBattleRunning)
        {
            HideBottomPanel();
            return;
        }

        UnitSpawner spawnerOnCell = FindSpawnerOnCell(cell);
        bool hasSpawner = spawnerOnCell != null;

        if (!hasSpawner)
        {
            ShowDeck();
        }
        else
        {
            // NEW: show upgrades for that spawner (same container), instead of hiding
            ShowUpgradesForSpawner(spawnerOnCell);
        }
    }

    /// <summary>
    /// Attempts to place a UnitSpawner on the currently selected cell.
    /// </summary>
    public void TryPlaceSpawner(UnitDefinition def)
    {
        if (def == null)
            return;

        if (battleManager != null && battleManager.IsBattleRunning)
            return;

        if (selectedCell == null)
            return;

        if (FindSpawnerOnCell(selectedCell) != null)
            return;

        int cost = def.soulCost;

        if (SoulsManager.instance == null)
        {
            Debug.LogError("[BattleCellSelectionController] SoulsManager.instance is null");
            return;
        }

        // Spend souls BEFORE spawning.
        if (!SoulsManager.instance.TrySpend(cost))
        {
            Debug.Log("[BattleCellSelectionController] Not enough souls. cost=" + cost);
            return;
        }

        if (spawnerPrefab == null)
        {
            Debug.LogError("[BattleCellSelectionController] spawnerPrefab is not assigned.");
            return;
        }

        UnitSpawner spawner = Instantiate(spawnerPrefab, selectedCell.transform.position, Quaternion.identity);

        int level = 1;
        if (battleManager != null)
            level = Mathf.Max(1, battleManager.playerUnitsLevel);

        spawner.Configure(def, Team.MyTeam, level);
        spawner.AttachCellProgressImage(selectedCell);

        // Apply special cell bonus multipliers.
        float hpMul = 1f;
        float dmgMul = 1f;

        if (selectedCell.IsSpecial)
        {
            if (selectedCell.bonusType == CellBonusType.HpPercent)
                hpMul = 1 + (selectedCell.percentValue / 100);
            else if (selectedCell.bonusType == CellBonusType.AttackPercent)
                dmgMul = 1 + (selectedCell.percentValue / 100);
        }

        spawner.SetCellBonusMultipliers(hpMul, dmgMul);
        battleManager?.RefreshStartBattleButton();

        // Per your flow: after placing, close the bottom deck and clear focus.
        ClearSelection();
    }

    // -------------------------
    // Internal selection helpers
    // -------------------------
    private void SetSelectedInternal(DropAreaCell cell)
    {
        if (selectedCell != null)
            selectedCell.SetSelected(false);

        selectedCell = cell;

        if (selectedCell != null)
            selectedCell.SetSelected(true);
    }

    private void ClearSelection()
    {
        if (selectedCell != null)
            selectedCell.SetSelected(false);

        selectedCell = null;
        HideBottomPanel();
    }

    private void ShowDeck()
    {
        TryResolveDeckUI();
        if (deckUI == null)
            return;

        // Show the deck panel itself
        deckUI.ShowDeck();
        deckUI.SetCardsInteractable(true);

        // Render deck content into the same container (Option A)
        if (bottomPanel != null)
        {
            var ui = FindFirstObjectByType<UnitSelectionUI>();
            if (ui != null && PlayerDeckProvider.Instance != null)
            {
                bottomPanel.ShowDeck(PlayerDeckProvider.Instance.CurrentDeck, ui);
            }
        }
    }

    private void HideBottomPanel()
    {
        TryResolveDeckUI();
        if (deckUI != null)
            deckUI.HideDeck();
    }

    private void ShowUpgradesForSpawner(UnitSpawner spawner)
    {
        TryResolveDeckUI();

        if (spawner == null)
        {
            HideBottomPanel();
            return;
        }

        if (bottomPanel == null)
        {
            // fallback
            HideBottomPanel();
            return;
        }

        // IMPORTANT: we need UnitDefinition from the spawner
        // Make sure UnitSpawner exposes it (see note below)
        UnitDefinition unit = spawner.unitDef;
        if (unit == null)
        {
            Debug.LogWarning("[BattleCellSelectionController] spawner.Unit is null. Expose UnitDefinition from UnitSpawner.Configure.");
            HideBottomPanel();
            return;
        }

        // ensure state exists
        var state = spawner.GetComponent<UnitSpawnerBattleUpgradeState>();
        if (state == null) state = spawner.gameObject.AddComponent<UnitSpawnerBattleUpgradeState>();

        deckUI.ShowDeck();
        deckUI.SetCardsInteractable(true);

        bottomPanel.ShowUpgrades(spawner, unit, state.currentTier);
    }

    /// <summary>
    /// Because DeckUIController lives in another scene, we resolve it dynamically.
    /// We also sync battleManager.deckUI once found.
    /// </summary>
    private void TryResolveDeckUI()
    {
        if (battleManager == null)
            battleManager = FindFirstObjectByType<BattleManager>();

        // If battleManager already has deckUI assigned, use it.
        if (deckUI == null && battleManager != null && battleManager.deckUI != null)
            deckUI = battleManager.deckUI;

        // Otherwise, try to find DeckUIController in loaded scenes.
        if (deckUI == null)
            deckUI = FindFirstObjectByType<DeckUIController>();

        // If we found it, keep it on battleManager as well.
        if (battleManager != null && battleManager.deckUI == null && deckUI != null)
            battleManager.deckUI = deckUI;

        // If deckUI exists but cardsParent is not assigned yet, try to assign from selectionUI.
        if (deckUI != null && deckUI.cardsParent == null)
        {
            var ui = FindFirstObjectByType<UnitSelectionUI>();
            if (ui != null && ui.buttonsParent != null)
                deckUI.cardsParent = ui.buttonsParent;
        }

        // NEW: resolve bottom panel controller from the same hierarchy as cardsParent
        if (bottomPanel == null && deckUI != null && deckUI.cardsParent != null)
        {
            bottomPanel = deckUI.cardsParent.GetComponentInParent<BattleBottomPanelController>();
            if (bottomPanel == null)
                bottomPanel = deckUI.cardsParent.GetComponent<BattleBottomPanelController>();
        }
    }

    private UnitSpawner FindSpawnerOnCell(DropAreaCell cell)
    {
        var all = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);
        foreach (var s in all)
        {
            if (s == null) continue;
            if (Vector2.Distance(s.transform.position, cell.transform.position) < 0.01f)
                return s;
        }
        return null;
    }
}
