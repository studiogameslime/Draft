using System.Collections;
using System.Collections.Generic;
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
    [SerializeField] private Vector2 cellWorldSize = new Vector2(0.5f, 0.5f);

    public bool IsTutorialActive => currentStep != TutorialStep.Complete && !tutorialComplete;
    public TutorialStep CurrentStep => currentStep;

    private TutorialStep currentStep = TutorialStep.Complete;
    private bool tutorialComplete = true;
    private TutorialOverlay overlay;
    private GameObject handPrefab;
    private Sprite kingSprite;
    private TutorialKingBubble kingBubble;

    // Audio
    private AudioSource audioSource;
    private Dictionary<TutorialStep, AudioClip> voiceClips;

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
        {
            handPrefab = config.handPrefab;
            kingSprite = config.kingSprite;
        }

        LoadVoiceClips();
    }

    private void LoadVoiceClips()
    {
        audioSource = gameObject.AddComponent<AudioSource>();
        audioSource.playOnAwake = false;

        voiceClips = new Dictionary<TutorialStep, AudioClip>();

        TutorialStep[] battleSteps = {
            TutorialStep.TapCell1, TutorialStep.ExplainSouls, TutorialStep.TapUnit1,
            TutorialStep.TapCell2, TutorialStep.TapUnit2, TutorialStep.TapStartBattle,
            TutorialStep.ExplainCapacity, TutorialStep.TapCell3, TutorialStep.TapUnit3,
            TutorialStep.TapStartBattle2, TutorialStep.TapBackToHome
        };

        foreach (var step in battleSteps)
        {
            var clip = Resources.Load<AudioClip>("Sounds/Tutorial/" + step);
            if (clip != null)
                voiceClips[step] = clip;
        }
    }

    private void PlayVoice(TutorialStep step)
    {
        if (audioSource == null || voiceClips == null) return;

        audioSource.Stop();

        if (voiceClips.TryGetValue(step, out var clip))
            audioSource.PlayOneShot(clip);
    }

    private IEnumerator WaitForGameDataAndInit()
    {
        while (GameData.Instance == null || !GameData.Instance.IsReady)
            yield return null;

        tutorialComplete = GameData.Instance.Save.tutorialComplete;

        if (tutorialComplete)
        {
            currentStep = TutorialStep.Complete;
        }
        else
        {
            currentStep = TutorialStep.TapCell1;
        }
    }

    // Called by BattleManager after InitAfterUIReady + PlanningPhase
    public void OnBattleReady()
    {
        if (tutorialComplete) return;

        string stageId = SceneManager.GetActiveScene().name;

        if (stageId == "1-1")
        {
            StartCoroutine(StartBattleTutorial());
        }
        else if (stageId == "1-2" && currentStep == TutorialStep.TapBonusCell)
        {
            StartCoroutine(StartBonusCellTutorial());
        }
    }

    private IEnumerator StartBattleTutorial()
    {
        yield return new WaitForSeconds(1.5f);

        EnsureOverlay();
        currentStep = TutorialStep.TapCell1;
        ShowCurrentStep();
    }

    private IEnumerator StartBonusCellTutorial()
    {
        yield return new WaitForSeconds(1.5f);

        EnsureOverlay();
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
                    advance = true;
                break;

            case TutorialStep.ExplainSouls:
                if (action == TutorialAction.TapToContinue) advance = true;
                break;

            case TutorialStep.TapUnit1:
            case TutorialStep.TapUnit2:
            case TutorialStep.TapUnit3:
                if (action == TutorialAction.UnitPlaced && context is UnitDefinition def && def == targetUnit)
                    advance = true;
                break;

            case TutorialStep.TapStartBattle:
            case TutorialStep.TapStartBattle2:
                if (action == TutorialAction.BattleStarted) advance = true;
                break;

            case TutorialStep.ExplainCapacity:
                if (action == TutorialAction.TapToContinue)
                {
                    Time.timeScale = 1f;
                    advance = true;
                }
                break;

            case TutorialStep.WaitForRound1:
            case TutorialStep.WaitForRound2:
                if (action == TutorialAction.RoundWon) advance = true;
                break;

            case TutorialStep.FreePlay:
                if (action == TutorialAction.LevelWon)
                {
                    // Skip WaitForVictory since LevelWon already happened
                    currentStep = TutorialStep.WaitForVictory;
                    advance = true;
                }
                break;

            case TutorialStep.WaitForVictory:
                if (action == TutorialAction.LevelWon) advance = true;
                break;

            case TutorialStep.TapBackToHome:
                if (action == TutorialAction.BackToHome) advance = true;
                break;

            // --- Home tutorial steps ---
            case TutorialStep.TapCollectionTab:
                if (action == TutorialAction.PageChanged && context is int pg1 && pg1 == (int)HomeScreenPage.Collection)
                    advance = true;
                break;

            case TutorialStep.TapUnitCard:
                if (action == TutorialAction.UnitCardClicked) advance = true;
                break;

            case TutorialStep.TapDetailsButton:
                if (action == TutorialAction.DetailsOpened) advance = true;
                break;

            case TutorialStep.TapUpgrade:
                if (action == TutorialAction.UnitUpgraded) advance = true;
                break;

            case TutorialStep.CloseUnitInfo:
                if (action == TutorialAction.UnitInfoClosed) advance = true;
                break;

            case TutorialStep.TapBattleTab:
                if (action == TutorialAction.PageChanged && context is int pg2 && pg2 == (int)HomeScreenPage.Battle)
                    advance = true;
                break;

            case TutorialStep.TapPlay:
                if (action == TutorialAction.PlayPressed) advance = true;
                break;

            // --- Battle 1-2 ---
            case TutorialStep.TapBonusCell:
                if (action == TutorialAction.BonusCellSelected) advance = true;
                break;
        }

        if (advance)
            AdvanceStep();
    }

    public bool IsActionAllowed(DropAreaCell cell)
    {
        if (!IsTutorialActive) return true;
        if (currentStep == TutorialStep.FreePlay) return true;
        switch (currentStep)
        {
            case TutorialStep.TapCell1:
            case TutorialStep.TapCell2:
            case TutorialStep.TapCell3:
            case TutorialStep.TapBonusCell:
                return cell == targetCell;
            default:
                return false;
        }
    }

    public bool IsActionAllowed(UnitDefinition def)
    {
        if (!IsTutorialActive) return true;
        if (currentStep == TutorialStep.FreePlay) return true;
        switch (currentStep)
        {
            case TutorialStep.TapUnit1:
            case TutorialStep.TapUnit2:
            case TutorialStep.TapUnit3:
                return def == targetUnit;
            default:
                return false;
        }
    }

    public bool IsStartBattleAllowed()
    {
        if (!IsTutorialActive) return true;
        return currentStep == TutorialStep.FreePlay
            || currentStep == TutorialStep.TapStartBattle
            || currentStep == TutorialStep.TapStartBattle2;
    }

    public bool IsPageAllowed(int pageIndex)
    {
        if (!IsTutorialActive) return true;
        if (currentStep == TutorialStep.FreePlay) return true;

        switch (currentStep)
        {
            case TutorialStep.TapCollectionTab:
                return pageIndex == (int)HomeScreenPage.Collection;
            case TutorialStep.TapBattleTab:
                return pageIndex == (int)HomeScreenPage.Battle;
            default:
                return false;
        }
    }

    // ============================
    // STEP MACHINE
    // ============================

    private void AdvanceStep()
    {
        currentStep++;
        Debug.Log($"[TUTORIAL] AdvanceStep -> {currentStep}");

        if (currentStep >= TutorialStep.Complete)
        {
            CompleteTutorial();
            return;
        }

        // Battle delays
        if (currentStep == TutorialStep.ExplainSouls)
        {
            StartCoroutine(ShowStepDelayed(0.3f));
            return;
        }

        if (currentStep == TutorialStep.TapUnit1 || currentStep == TutorialStep.TapUnit2 || currentStep == TutorialStep.TapUnit3)
        {
            StartCoroutine(ShowStepDelayed(0.3f));
            return;
        }

        if (currentStep == TutorialStep.TapCell3)
        {
            StartCoroutine(WaitForFooterThenShow());
            return;
        }

        if (currentStep == TutorialStep.ExplainCapacity)
        {
            // Hide overlay so the player can watch the battle unfold
            if (overlay != null) overlay.Hide();
            if (kingBubble != null) kingBubble.Hide();
            StartCoroutine(WaitForReserveReady());
            return;
        }

        // TapBonusCell: don't show yet, wait for OnBattleReady in 1-2
        if (currentStep == TutorialStep.TapBonusCell)
        {
            BlockInput();
            return;
        }

        // Home step delays for UI transitions
        if (currentStep == TutorialStep.TapUnitCard ||
            currentStep == TutorialStep.TapDetailsButton ||
            currentStep == TutorialStep.TapUpgrade ||
            currentStep == TutorialStep.CloseUnitInfo ||
            currentStep == TutorialStep.TapBattleTab)
        {
            StartCoroutine(ShowStepDelayed(0.5f));
            return;
        }

        ShowCurrentStep();
    }

    private IEnumerator ShowStepDelayed(float delay)
    {
        BlockInput();
        yield return new WaitForSeconds(delay);
        ShowCurrentStep();
    }

    private IEnumerator WaitForFooterThenShow()
    {
        BlockInput();

        // Wait for the footer slide-up animation to finish
        yield return new WaitForSeconds(0.5f);
        while (BattleFooterAnimation.instance != null && BattleFooterAnimation.instance.IsAnimating)
            yield return null;
        yield return new WaitForSeconds(0.3f);

        ShowCurrentStep();
    }

    private void BlockInput()
    {
        EnsureOverlay();
        overlay.ShowDarkPanelOnly();
        if (kingBubble != null) kingBubble.Hide();
    }

    private void ShowCurrentStep()
    {
        Debug.Log($"[TUTORIAL] ShowCurrentStep: step={currentStep}, overlay={overlay != null}");
        if (overlay == null) return;

        targetCell = null;
        targetUnit = null;

        switch (currentStep)
        {
            // --- Battle 1-1 ---
            case TutorialStep.TapCell1:
                ShowCellStep(cell1Row, cell1Col);
                break;
            case TutorialStep.ExplainSouls:
                ShowExplainSoulsStep();
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
            case TutorialStep.WaitForRound2:
            case TutorialStep.FreePlay:
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

            // --- Home ---
            case TutorialStep.TapCollectionTab:
            case TutorialStep.TapBattleTab:
                ShowTabStep();
                break;
            case TutorialStep.TapUnitCard:
                ShowUnitCardStep();
                break;
            case TutorialStep.TapDetailsButton:
                ShowDetailsButtonStep();
                break;
            case TutorialStep.TapUpgrade:
                ShowUpgradeButtonStep();
                break;
            case TutorialStep.CloseUnitInfo:
                ShowCloseUnitInfoStep();
                break;
            case TutorialStep.TapPlay:
                ShowPlayStep();
                break;

            // --- Battle 1-2 ---
            case TutorialStep.TapBonusCell:
                ShowBonusCellStep();
                break;
        }

        // King bubble + voice
        string msg = GetStepMessage(currentStep);
        if (msg != null)
        {
            EnsureKingBubble();
            kingBubble.Show(msg);
            PlayVoice(currentStep);
        }
        else if (kingBubble != null)
        {
            kingBubble.Hide();
        }
    }

    private string GetStepMessage(TutorialStep step)
    {
        switch (step)
        {
            // Battle 1-1
            case TutorialStep.TapCell1: return "Welcome, Commander! Tap this cell to choose where your unit will stand.";
            case TutorialStep.ExplainSouls: return "See those souls? Every unit costs souls to deploy. Use them wisely... or you'll run out!";
            case TutorialStep.TapUnit1: return "Nice spot! Now pick the Warrior to hold the line.";
            case TutorialStep.TapCell2: return "One unit won't be enough... Tap another cell!";
            case TutorialStep.TapUnit2: return "Let's add some range! Deploy the Archer here.";
            case TutorialStep.TapStartBattle: return "Your squad is ready. Hit the button and let the battle begin!";
            case TutorialStep.ExplainCapacity: return "See that? Each unit has a reserve! When one falls, the next one steps up to fight.";
            case TutorialStep.TapCell3: return "The enemy is getting stronger. Place one more unit!";
            case TutorialStep.TapUnit3: return "Another Warrior should do the trick. Deploy!";
            case TutorialStep.TapStartBattle2: return "This is the final wave. Show them what you've got!";
            case TutorialStep.TapBackToHome: return "Victory! You're a natural. Collect your rewards!";

            // Home
            case TutorialStep.TapCollectionTab: return "Great job on your first battle! But the next one is tougher. Let's power up your warriors!";
            case TutorialStep.TapUnitCard: return "Tap on the Warrior to check it out.";
            case TutorialStep.TapDetailsButton: return "Tap Details to see its full stats and upgrade options.";
            case TutorialStep.TapUpgrade: return "Upgrade your unit to make it stronger for the next fight!";
            case TutorialStep.CloseUnitInfo: return "Nice! Your warrior is stronger now. Let's get back to battle!";
            case TutorialStep.TapBattleTab: return "Head back to the Battle screen.";
            case TutorialStep.TapPlay: return "You're ready, Commander. Let's take on the next challenge!";

            // Battle 1-2
            case TutorialStep.TapBonusCell: return "See this green cell? Place a unit here for a special bonus! Look for colored cells in every battle.";

            default: return null;
        }
    }

    private void EnsureKingBubble()
    {
        if (kingBubble != null) return;

        var go = new GameObject("TutorialKingBubble");
        DontDestroyOnLoad(go);
        kingBubble = go.AddComponent<TutorialKingBubble>();
        kingBubble.Init(kingSprite);
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
        yield return new WaitForSeconds(1f);

        EnsureOverlay();

        if (EndGameUI.Instance != null)
        {
            var buttons = EndGameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                string name = btn.gameObject.name;
                if (name == "CollectButton")
                {
                    overlay.SetTargetUI(btn.GetComponent<RectTransform>());
                    yield break;
                }
            }
        }
    }

    // ============================
    // INFO STEP HELPERS
    // ============================

    private void ShowExplainSoulsStep()
    {
        if (SoulsManager.instance != null && SoulsManager.instance._currentSoulsText != null)
        {
            var parent = SoulsManager.instance._currentSoulsText.transform.parent;
            var rt = parent != null ? parent.GetComponent<RectTransform>() : null;
            if (rt != null)
                overlay.SetTargetUI(rt, showHand: false);
            else
                overlay.ShowDarkPanelOnly();
        }
        else
        {
            overlay.ShowDarkPanelOnly();
        }
        overlay.EnableTapToContinue();
    }

    private IEnumerator WaitForReserveReady()
    {
        while (true)
        {
            var spawners = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);
            foreach (var s in spawners)
            {
                if (s.HasReserveWaiting)
                {
                    Time.timeScale = 0f;
                    EnsureOverlay();
                    overlay.SetTargetWorld(s.transform.position, cellWorldSize);
                    overlay.EnableTapToContinue();

                    EnsureKingBubble();
                    kingBubble.Show(GetStepMessage(TutorialStep.ExplainCapacity));
                    PlayVoice(TutorialStep.ExplainCapacity);
                    yield break;
                }
            }
            yield return null;
        }
    }

    // ============================
    // HOME STEP HELPERS
    // ============================

    private void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        Debug.Log($"[TUTORIAL] OnSceneLoaded({scene.name}), mode={mode}, IsTutorialActive={IsTutorialActive}, step={currentStep}");
        if (!IsTutorialActive) return;

        // Home tutorial: trigger when arriving at HomeScreen after battle
        if (scene.name == "HomeScreen" && mode == LoadSceneMode.Single
            && currentStep >= TutorialStep.TapCollectionTab && currentStep <= TutorialStep.TapPlay)
        {
            StartCoroutine(ShowHomeStep());
        }
    }

    public void StartHomeTutorial()
    {
        Debug.Log($"[TUTORIAL] StartHomeTutorial() called, step={currentStep}");
        StartCoroutine(ShowHomeStep());
    }

    private IEnumerator ShowHomeStep()
    {
        yield return new WaitForSeconds(0.5f);

        EnsureOverlay();
        ShowCurrentStep();
    }

    private void ShowTabStep()
    {
        var scrollSnap = FindFirstObjectByType<ScrollSnap>();
        if (scrollSnap == null) return;

        int tabIndex = -1;
        switch (currentStep)
        {
            case TutorialStep.TapCollectionTab: tabIndex = (int)HomeScreenPage.Collection; break;
            case TutorialStep.TapBattleTab: tabIndex = (int)HomeScreenPage.Battle; break;
        }

        if (tabIndex < 0) return;

        var btn = scrollSnap.GetBottomButton(tabIndex);
        if (btn != null)
            overlay.SetTargetUI(btn.GetComponent<RectTransform>());
    }

    private void ShowUnitCardStep()
    {
        // Find the Warrior card in the collection (not the deck)
        var cards = FindObjectsByType<UnitCardView>(FindObjectsSortMode.None);
        foreach (var card in cards)
        {
            if (card.Definition != null && card.Definition.id == "unit_warrior" && !card.IsDeckSlot)
            {
                overlay.SetTargetUI(card.RectTransform);
                return;
            }
        }
    }

    private void ShowDetailsButtonStep()
    {
        // The card expands and shows quick actions with an "Upgrade" button
        // which actually opens the details popup. Find the expanded card's upgrade button.
        var gridCtrl = UnitsGridController.Instance;
        if (gridCtrl == null) return;

        // Find buttons labelled "Details" or "Upgrade" in the expanded card area
        var cards = FindObjectsByType<UnitCardView>(FindObjectsSortMode.None);
        foreach (var card in cards)
        {
            if (card.Definition == null) continue;

            // Look for the quickActionsPanel's upgrade button (which opens details)
            var buttons = card.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                string name = btn.gameObject.name.ToLower();
                if (name.Contains("upgrade") || name.Contains("detail"))
                {
                    if (btn.gameObject.activeInHierarchy)
                    {
                        overlay.SetTargetUI(btn.GetComponent<RectTransform>());
                        return;
                    }
                }
            }
        }
    }

    private void ShowUpgradeButtonStep()
    {
        if (UnitDetailsPopupController.Instance == null) return;

        var btn = UnitDetailsPopupController.Instance.upgradeButton;
        if (btn != null)
            overlay.SetTargetUI(btn.GetComponent<RectTransform>());
    }

    private void ShowCloseUnitInfoStep()
    {
        if (UnitDetailsPopupController.Instance == null) return;

        var buttons = UnitDetailsPopupController.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true);
        foreach (var btn in buttons)
        {
            string name = btn.gameObject.name.ToLower();
            if (name.Contains("close"))
            {
                overlay.SetTargetUI(btn.GetComponent<RectTransform>());
                return;
            }
        }
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

    private void ShowBonusCellStep()
    {
        if (BattleManager.instance == null || BattleManager.instance.dropAreaGrids == null ||
            BattleManager.instance.dropAreaGrids.Length == 0)
            return;

        // Find the first bonus cell in any grid
        foreach (var grid in BattleManager.instance.dropAreaGrids)
        {
            var cells = grid.GetComponentsInChildren<DropAreaCell>();
            foreach (var cell in cells)
            {
                if (cell.IsSpecial)
                {
                    targetCell = cell;
                    overlay.SetTargetWorld(cell.transform.position, cellWorldSize);
                    return;
                }
            }
        }
    }

    // ============================
    // COMPLETION
    // ============================

    private void CompleteTutorial()
    {
        Debug.Log("[TUTORIAL] CompleteTutorial()");
        tutorialComplete = true;
        currentStep = TutorialStep.Complete;

        if (audioSource != null) audioSource.Stop();

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

        if (kingBubble != null)
        {
            kingBubble.Hide();
            Destroy(kingBubble.gameObject);
            kingBubble = null;
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
