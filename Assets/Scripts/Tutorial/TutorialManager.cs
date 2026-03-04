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
            else
                Debug.LogWarning($"[Tutorial] Missing voice clip for {step}");
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

            case TutorialStep.ExplainSouls:
                if (action == TutorialAction.TapToContinue) advance = true;
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

            case TutorialStep.ExplainCapacity:
                if (action == TutorialAction.TapToContinue)
                {
                    Time.timeScale = 1f;
                    advance = true;
                }
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
        switch (currentStep)
        {
            case TutorialStep.TapCell1:
            case TutorialStep.TapCell2:
            case TutorialStep.TapCell3:
                return cell == targetCell;
            case TutorialStep.TapUnit1:
            case TutorialStep.TapUnit2:
            case TutorialStep.TapUnit3:
            case TutorialStep.ExplainSouls:
            case TutorialStep.ExplainCapacity:
                return false;
            default:
                return true;
        }
    }

    public bool IsActionAllowed(UnitDefinition def)
    {
        if (!IsTutorialActive) return true;
        switch (currentStep)
        {
            case TutorialStep.TapUnit1:
            case TutorialStep.TapUnit2:
            case TutorialStep.TapUnit3:
                return def == targetUnit;
            case TutorialStep.TapCell1:
            case TutorialStep.TapCell2:
            case TutorialStep.TapCell3:
            case TutorialStep.ExplainSouls:
            case TutorialStep.ExplainCapacity:
                return false;
            default:
                return true;
        }
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

        // Skip home screen steps — end tutorial after collecting rewards
        if (currentStep >= TutorialStep.TapMissions)
        {
            CompleteTutorial();
            return;
        }

        // ExplainSouls: wait for cell selection UI to settle
        if (currentStep == TutorialStep.ExplainSouls)
        {
            StartCoroutine(ShowStepDelayed(0.3f));
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

        // ExplainCapacity: start monitoring coroutine instead of showing immediately
        if (currentStep == TutorialStep.ExplainCapacity)
        {
            overlay.Hide();
            if (kingBubble != null) kingBubble.Hide();
            StartCoroutine(WaitForReserveReady());
            return;
        }

        ShowCurrentStep();
    }

    private IEnumerator ShowStepDelayed(float delay)
    {
        if (overlay != null) overlay.Hide();
        if (kingBubble != null) kingBubble.Hide();
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
                ShowMissionsStep();
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

        // Show/hide king bubble + play voice
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
            case TutorialStep.TapMissions: return "Check your missions here. Complete them to earn extra rewards!";
            case TutorialStep.CloseMissions: return "Got it! Close this and let's keep exploring.";
            case TutorialStep.TapStoreTab: return "Head to the Store! This is where you unlock new units and power-ups...";
            case TutorialStep.TapCollectionTab: return "This is your Collection. See all the units you've gathered!";
            case TutorialStep.TapBattleTab: return "Back to Battle! This is where you pick your next fight.";
            case TutorialStep.TapPlay: return "You're all set, Commander. Tap Play and lead your army!";
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
        // Wait for EndGameUI to appear
        yield return new WaitForSeconds(1f);

        EnsureOverlay();

        if (EndGameUI.Instance != null)
        {
            var buttons = EndGameUI.Instance.GetComponentsInChildren<UnityEngine.UI.Button>(true);
            foreach (var btn in buttons)
            {
                // Target the "CollectButton", not the "AdCollectButton" (X2)
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
                overlay.SetTargetUI(rt);
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
                    // Found a reserve — pause and show info
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
        ShowCurrentStep();
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
