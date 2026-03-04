using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }

    [Header("Battle Step Config – Round 1")]
    [SerializeField] private int cell1Row = 0;
    [SerializeField] private int cell1Col = 1;
    [SerializeField] private int cell2Row = 1;
    [SerializeField] private int cell2Col = 1;
    [SerializeField] private int unit1DeckIndex = 1;
    [SerializeField] private int unit2DeckIndex = 0;

    [Header("Battle Step Config – Round 2")]
    [SerializeField] private int cell3Row = 0;
    [SerializeField] private int cell3Col = 0;
    [SerializeField] private int unit3DeckIndex = 1;

    [Header("Cell Highlight Size (world units)")]
    [SerializeField] private Vector2 cellWorldSize = new Vector2(0.7f, 0.7f);

    public bool IsTutorialActive => currentStep != TutorialStep.Complete && !tutorialComplete;
    public TutorialStep CurrentStep => currentStep;

    private TutorialStep currentStep = TutorialStep.Complete;
    private bool tutorialComplete = true;
    private TutorialOverlay overlay;
    private GameObject handPrefab;

    // Cached targets for IsActionAllowed checks
    private DropAreaCell targetCell;
    private UnitDefinition targetUnit;

    [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
    private static void AutoCreate()
    {
        if (Instance != null) return;

        var go = new GameObject("TutorialManager");
        Instance = go.AddComponent<TutorialManager>();
        DontDestroyOnLoad(go);
    }

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        SceneManager.sceneLoaded += OnSceneLoaded;
    }

    private void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded;
        if (Instance == this) Instance = null;
    }

    private void Start()
    {
        LoadConfig();
        StartCoroutine(WaitForGameDataAndInit());
    }

    private void LoadConfig()
    {
        var config = Resources.Load<TutorialConfig>("TutorialConfig");
        if (config != null)
            handPrefab = config.handPrefab;
    }

    private IEnumerator WaitForGameDataAndInit()
    {
        while (GameData.Instance == null || !GameData.Instance.IsReady)
            yield return null;

        tutorialComplete = GameData.Instance.Save.tutorialComplete;
        Debug.Log($"[Tutorial] tutorialComplete = {tutorialComplete}");

        if (tutorialComplete)
        {
            currentStep = TutorialStep.Complete;
        }
        else
        {
            currentStep = TutorialStep.TapCell1;

            // If we're already on HomeScreen, redirect to 1-1
            if (SceneManager.GetActiveScene().name == "HomeScreen")
            {
                yield return new WaitForSeconds(0.3f);
                SceneManager.LoadScene("1-1");
            }
        }
    }

    // Called by BattleManager after InitAfterUIReady + PlanningPhase
    public void OnBattleReady()
    {
        if (tutorialComplete) return;

        string stageId = SceneManager.GetActiveScene().name;
        if (stageId != "1-1") return;

        StartCoroutine(StartBattleTutorial());
    }

    private IEnumerator StartBattleTutorial()
    {
        // Wait for footer slide-up animation (1.2s) to finish
        yield return new WaitForSeconds(1.5f);

        EnsureOverlay();
        currentStep = TutorialStep.TapCell1;
        ShowCurrentStep();
    }

    // ============================
    // NOTIFY / ALLOWED
    // ============================

    public void NotifyAction(TutorialAction action, object context = null)
    {
        if (!IsTutorialActive) return;

        bool advance = false;

        switch (currentStep)
        {
            case TutorialStep.TapCell1:
            case TutorialStep.TapCell2:
            case TutorialStep.TapCell3:
                if (action == TutorialAction.CellSelected && context is DropAreaCell cell && cell == targetCell)
                {
                    advance = true;
                }
                break;

            case TutorialStep.TapUnit1:
            case TutorialStep.TapUnit2:
            case TutorialStep.TapUnit3:
                if (action == TutorialAction.UnitPlaced && context is UnitDefinition def && def == targetUnit)
                {
                    advance = true;
                }
                break;

            case TutorialStep.TapStartBattle:
            case TutorialStep.TapStartBattle2:
                if (action == TutorialAction.BattleStarted) advance = true;
                break;

            case TutorialStep.WaitForRound1:
                if (action == TutorialAction.RoundWon) advance = true;
                break;

            case TutorialStep.WaitForVictory:
                if (action == TutorialAction.LevelWon) advance = true;
                break;

            case TutorialStep.TapBackToHome:
                if (action == TutorialAction.BackToHome) advance = true;
                break;

            case TutorialStep.TapMissions:
                if (action == TutorialAction.MissionsOpened) advance = true;
                break;

            case TutorialStep.CloseMissions:
                if (action == TutorialAction.MissionsClosed) advance = true;
                break;

            case TutorialStep.TapStoreTab:
                if (action == TutorialAction.PageChanged && context is int page0 && page0 == (int)HomeScreenPage.Store)
                    advance = true;
                break;

            case TutorialStep.TapCollectionTab:
                if (action == TutorialAction.PageChanged && context is int page1 && page1 == (int)HomeScreenPage.Collection)
                    advance = true;
                break;

            case TutorialStep.TapBattleTab:
                if (action == TutorialAction.PageChanged && context is int page2 && page2 == (int)HomeScreenPage.Battle)
                    advance = true;
                break;

            case TutorialStep.TapPlay:
                if (action == TutorialAction.PlayPressed) advance = true;
                break;
        }

        if (advance)
            AdvanceStep();
    }

    public bool IsActionAllowed(DropAreaCell cell)
    {
        if (!IsTutorialActive) return true;
        if (currentStep == TutorialStep.TapCell1 || currentStep == TutorialStep.TapCell2 || currentStep == TutorialStep.TapCell3)
            return cell == targetCell;
        return false;
    }

    public bool IsActionAllowed(UnitDefinition def)
    {
        if (!IsTutorialActive) return true;
        if (currentStep == TutorialStep.TapUnit1 || currentStep == TutorialStep.TapUnit2 || currentStep == TutorialStep.TapUnit3)
            return def == targetUnit;
        return false;
    }

    public bool IsPageAllowed(int pageIndex)
    {
        if (!IsTutorialActive) return true;

        switch (currentStep)
        {
            case TutorialStep.TapStoreTab:
                return pageIndex == (int)HomeScreenPage.Store;
            case TutorialStep.TapCollectionTab:
                return pageIndex == (int)HomeScreenPage.Collection;
            case TutorialStep.TapBattleTab:
                return pageIndex == (int)HomeScreenPage.Battle;
            default:
                return true;
        }
    }

    // ============================
    // STEP MACHINE
    // ============================

    private void AdvanceStep()
    {
        currentStep++;

        if (currentStep == TutorialStep.Complete)
        {
            CompleteTutorial();
            return;
        }

        // For unit steps, wait for deck UI to populate
        if (currentStep == TutorialStep.TapUnit1 || currentStep == TutorialStep.TapUnit2 || currentStep == TutorialStep.TapUnit3)
        {
            StartCoroutine(ShowStepDelayed(0.3f));
            return;
        }

        // After round 1 win, wait for round transition + footer animation
        if (currentStep == TutorialStep.TapCell3)
        {
            StartCoroutine(ShowStepDelayed(4f));
            return;
        }

        ShowCurrentStep();
    }

    private IEnumerator ShowStepDelayed(float delay)
    {
        if (overlay != null) overlay.Hide();
        yield return new WaitForSeconds(delay);
        ShowCurrentStep();
    }

    private void ShowCurrentStep()
    {
        if (overlay == null) return;

        targetCell = null;
        targetUnit = null;

        switch (currentStep)
        {
            case TutorialStep.TapCell1:
                ShowCellStep(cell1Row, cell1Col);
                break;

            case TutorialStep.TapUnit1:
                ShowUnitStep(unit1DeckIndex);
                break;

            case TutorialStep.TapCell2:
                ShowCellStep(cell2Row, cell2Col);
                break;

            case TutorialStep.TapUnit2:
                ShowUnitStep(unit2DeckIndex);
                break;

            case TutorialStep.TapStartBattle:
            case TutorialStep.TapStartBattle2:
                ShowStartBattleStep();
                break;

            case TutorialStep.WaitForRound1:
            case TutorialStep.WaitForVictory:
                overlay.Hide();
                break;

            case TutorialStep.TapCell3:
                ShowCellStep(cell3Row, cell3Col);
                break;

            case TutorialStep.TapUnit3:
                ShowUnitStep(unit3DeckIndex);
                break;

            case TutorialStep.TapBackToHome:
                StartCoroutine(ShowBackToHomeStep());
                break;

            case TutorialStep.TapMissions:
                // Shown after scene load via OnSceneLoaded
                break;

            case TutorialStep.CloseMissions:
                StartCoroutine(ShowCloseMissionsStep());
                break;

            case TutorialStep.TapStoreTab:
            case TutorialStep.TapCollectionTab:
            case TutorialStep.TapBattleTab:
                ShowTabStep();
                break;

            case TutorialStep.TapPlay:
                ShowPlayStep();
                break;
        }
    }

    // ============================
    // BATTLE STEP HELPERS
    // ============================

    private void ShowCellStep(int row, int col)
    {
        if (BattleManager.instance == null || BattleManager.instance.dropAreaGrids == null ||
            BattleManager.instance.dropAreaGrids.Length == 0)
            return;

        var grid = BattleManager.instance.dropAreaGrids[0];
        targetCell = grid.GetCell(row, col);
        if (targetCell == null) return;

        overlay.SetTargetWorld(targetCell.transform.position, cellWorldSize);
    }

    private void ShowUnitStep(int deckIndex)
    {
        var buttons = FindObjectsByType<UnitSpawnButton>(FindObjectsSortMode.None);
        if (PlayerDeckProvider.Instance == null || PlayerDeckProvider.Instance.CurrentDeck == null)
            return;

        var deck = PlayerDeckProvider.Instance.CurrentDeck;
        if (deckIndex >= deck.Count) return;

        targetUnit = deck[deckIndex];

        foreach (var btn in buttons)
        {
            if (btn.unitDefinition == targetUnit)
            {
                overlay.SetTargetUI(btn.GetComponent<RectTransform>());
                return;
            }
        }
    }

    private void ShowStartBattleStep()
    {
        if (StartBattleButton.instance == null || StartBattleButton.instance.startButton == null)
            return;

        var rt = StartBattleButton.instance.startButton.GetComponent<RectTransform>();
        overlay.SetTargetUI(rt);
    }

    private IEnumerator ShowBackToHomeStep()
    {
        // Wait for EndGameUI to appear
        yield return new WaitForSeconds(1f);

        EnsureOverlay();

        // Find the "Back to Home" button - it's usually a Button on EndGameUI
        // We look for a child button that calls BackToHome
        if (EndGameUI.Instance != null)
        {
            var buttons = EndGameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                // Find button with "BackToHome" or "Home" in name
                string name = btn.gameObject.name.ToLower();
                if (name.Contains("home") || name.Contains("back"))
                {
                    overlay.SetTargetUI(btn.GetComponent<RectTransform>());
                    yield break;
                }
            }

            // Fallback: use the last button (often the home button)
            if (buttons.Length > 0)
            {
                overlay.SetTargetUI(buttons[buttons.Length - 1].GetComponent<RectTransform>());
            }
        }
    }

    // ============================
    // HOME STEP HELPERS
    // ============================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        if (!IsTutorialActive) return;

        if (scene.name == "HomeScreen")
        {
            if (currentStep < TutorialStep.TapMissions)
            {
                // Battle phase: redirect fresh player to 1-1
                StartCoroutine(RedirectToBattle());
            }
            else
            {
                StartCoroutine(ShowHomeStep());
            }
        }
    }

    private IEnumerator RedirectToBattle()
    {
        // Wait for GameData to be ready
        while (GameData.Instance == null || !GameData.Instance.IsReady)
            yield return null;

        // Small delay for HomeScreen to settle before redirecting
        yield return new WaitForSeconds(0.3f);

        currentStep = TutorialStep.TapCell1;
        SceneManager.LoadScene("1-1");
    }

    private IEnumerator ShowHomeStep()
    {
        // Wait for home screen to initialize
        yield return new WaitForSeconds(0.5f);

        EnsureOverlay();

        switch (currentStep)
        {
            case TutorialStep.TapMissions:
                ShowMissionsStep();
                break;
            case TutorialStep.TapStoreTab:
            case TutorialStep.TapCollectionTab:
            case TutorialStep.TapBattleTab:
                ShowTabStep();
                break;
            case TutorialStep.TapPlay:
                ShowPlayStep();
                break;
        }
    }

    private void ShowMissionsStep()
    {
        var missionsBtn = FindFirstObjectByType<MissionsButton>();
        if (missionsBtn == null) return;

        var rt = missionsBtn.GetComponent<RectTransform>();
        if (rt == null)
        {
            var btn = missionsBtn.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null) rt = btn.GetComponent<RectTransform>();
        }

        if (rt != null)
            overlay.SetTargetUI(rt);
    }

    private IEnumerator ShowCloseMissionsStep()
    {
        yield return new WaitForSeconds(0.5f);

        EnsureOverlay();

        // Find the missions screen close area (the overlay/background that closes on tap)
        var missionsScreen = FindFirstObjectByType<MissionsScreen>();
        if (missionsScreen == null) yield break;

        // Look for a close button inside the missions panel
        var buttons = missionsScreen.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            string name = btn.gameObject.name.ToLower();
            if (name.Contains("close") || name.Contains("overlay") || name.Contains("back"))
            {
                overlay.SetTargetUI(btn.GetComponent<RectTransform>());
                yield break;
            }
        }

        // Fallback: highlight the whole screen area so player taps overlay to close
        if (buttons.Length > 0)
            overlay.SetTargetUI(buttons[0].GetComponent<RectTransform>());
    }

    private void ShowTabStep()
    {
        var scrollSnap = FindFirstObjectByType<ScrollSnap>();
        if (scrollSnap == null) return;

        int tabIndex = -1;
        switch (currentStep)
        {
            case TutorialStep.TapStoreTab: tabIndex = (int)HomeScreenPage.Store; break;
            case TutorialStep.TapCollectionTab: tabIndex = (int)HomeScreenPage.Collection; break;
            case TutorialStep.TapBattleTab: tabIndex = (int)HomeScreenPage.Battle; break;
        }

        if (tabIndex < 0) return;

        var btn = scrollSnap.GetBottomButton(tabIndex);
        if (btn != null)
            overlay.SetTargetUI(btn.GetComponent<RectTransform>());
    }

    private void ShowPlayStep()
    {
        var playBtn = FindFirstObjectByType<PlayButton>();
        if (playBtn == null) return;

        var rt = playBtn.GetComponent<RectTransform>();
        if (rt == null)
        {
            var btn = playBtn.GetComponentInChildren<UnityEngine.UI.Button>();
            if (btn != null) rt = btn.GetComponent<RectTransform>();
        }

        if (rt != null)
            overlay.SetTargetUI(rt);
    }

    // ============================
    // COMPLETION
    // ============================

    private void CompleteTutorial()
    {
        tutorialComplete = true;
        currentStep = TutorialStep.Complete;

        if (GameData.Instance != null && GameData.Instance.Save != null)
        {
            GameData.Instance.Save.tutorialComplete = true;
            GameData.Instance.SaveNow();
        }

        if (overlay != null)
        {
            overlay.Hide();
            Destroy(overlay.gameObject);
            overlay = null;
        }
    }

    // ============================
    // OVERLAY
    // ============================

    private void EnsureOverlay()
    {
        if (overlay != null) return;

        var go = new GameObject("TutorialOverlay");
        DontDestroyOnLoad(go);
        overlay = go.AddComponent<TutorialOverlay>();
        overlay.Init(handPrefab);
    }

}
