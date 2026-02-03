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
        // Do not assume deckUI exists yet (CommonUI might not be loaded).
        TryResolveDeckUI();

        // If deckUI exists, hide it; otherwise it will be hidden once it is resolved.
        HideBottomPanel();
    }

    private void Update()
    {
        TryResolveDeckUI();

        if (!Application.isPlaying) return;
        if (BattleManager.instance != null && BattleManager.instance.IsBattleRunning) return;

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
        if (BattleManager.instance.IsBattleRunning) return;

        if (cell == null)
            return;

        // Clicking the same cell again toggles selection off (hides deck).
        if (selectedCell == cell)
        {
            ClearSelection();
            return;
        }

        SetSelectedInternal(cell);

        if (BattleManager.instance != null && BattleManager.instance.IsBattleRunning)
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

        if (BattleManager.instance != null && BattleManager.instance.IsBattleRunning)
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
            ToastManager.Instance.Show("Not enough souls!");
            Debug.Log("[BattleCellSelectionController] Not enough souls. cost=" + cost);
            return;
        }

        if (spawnerPrefab == null)
        {
            Debug.LogError("[BattleCellSelectionController] spawnerPrefab is not assigned.");
            return;
        }

        UnitSpawner spawner = Instantiate(spawnerPrefab, selectedCell.transform.position, Quaternion.identity, selectedCell.transform);

        int lvl = UnitLevelingService.GetUnitLevel(def.id);
        spawner.Configure(def, Team.MyTeam, lvl);

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
        BattleManager.instance?.RefreshStartBattleButton();

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

    public void ClearSelection()
    {
        if (selectedCell != null)
            selectedCell.SetSelected(false);

        selectedCell = null;
        HideBottomPanel();
    }

    private void ShowDeck()
    {
        TryResolveDeckUI();
        if (deckUI != null)
        {
            deckUI.ShowDeck();
            deckUI.SetCardsInteractable(true);
        }

        if (bottomPanel != null && PlayerDeckProvider.Instance != null)
        {
            bottomPanel.ShowDeck(PlayerDeckProvider.Instance.CurrentDeck);
            bottomPanel.HidePlaceholderText();
            bottomPanel.HideSellButton();
        }
    }


    private void HideBottomPanel()
    {
        TryResolveDeckUI();

        if (deckUI != null)
            deckUI.HideDeck();

        if (bottomPanel != null)
        {
            bottomPanel.ShowPlaceholderText(bottomPanel.defaultPlaceholder);
            bottomPanel.HideSellButton();
        }
    }


    private void ShowUpgradesForSpawner(UnitSpawner spawner)
    {
        TryResolveDeckUI();
        if (spawner == null || bottomPanel == null)
        {
            HideBottomPanel();
            return;
        }

        UnitDefinition unit = spawner.unitDef;
        if (unit == null)
        {
            Debug.LogWarning("[BattleCellSelectionController] spawner.unitDef is null");
            HideBottomPanel();
            return;
        }

        var state = spawner.GetComponent<UnitSpawnerBattleUpgradeState>();
        if (state == null)
            state = spawner.gameObject.AddComponent<UnitSpawnerBattleUpgradeState>();

        if (deckUI != null)
        {
            deckUI.ShowDeck();
            deckUI.SetCardsInteractable(true);
        }

        bottomPanel.ShowUpgrades(spawner, unit, state.currentTier);
    }


    /// <summary>
    /// Because DeckUIController lives in another scene, we resolve it dynamically.
    /// We also sync battleManager.deckUI once found.
    /// </summary>
    private void TryResolveDeckUI()
    {
        if (deckUI == null && BattleManager.instance != null && BattleManager.instance.deckUI != null)
            deckUI = BattleManager.instance.deckUI;

        if (deckUI == null)
            deckUI = FindFirstObjectByType<DeckUIController>();

        if (BattleManager.instance != null && BattleManager.instance.deckUI == null && deckUI != null)
            BattleManager.instance.deckUI = deckUI;

        if (bottomPanel == null)
            bottomPanel = FindFirstObjectByType<BattleBottomPanelController>();
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
