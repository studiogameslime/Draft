using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    public static BattleManager instance;


    [Header("Level Data")]
    public LevelDefinition levelDefinition;

    [Header("Player unit level (from meta progression)")]
    public int playerUnitsLevel = 1;

    [Header("Timing")]
    public float startBattleDelay = 2f;

    [Header("Deck UI Manager")]
    public DeckUIController deckUI;

    [Header("Drop Areas")]
    public DropAreaGrid[] dropAreaGrids;

    [Header("Enemy")]
    public EnemyWaveSpawner enemyWaveSpawner;

    // =======================
    // State
    // =======================
    private bool battleStarted = false;
    private bool gameOver = false;
    private bool waitingForRoundEnd = false;

    public bool IsExitingBattle { get; private set; }
    [HideInInspector] public int currentRoundIndex = 0;

    private HashSet<UnitClass> unitClassUsedThisBattle = new();
    private HashSet<UnitDefinition> unitsUsedThisBattle = new();

    public bool IsBattleRunning => battleStarted;
    public bool IsGameOver => gameOver;

    public WallHealth wallHealth;

    // =======================
    // START
    // =======================

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        StartCoroutine(InitAfterUIReady());
        Initialize();


    }

    public void Initialize()
    {
        SetAllAIEnabled(false);
        enemyWaveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        dropAreaGrids = FindObjectsByType<DropAreaGrid>(FindObjectsSortMode.None);

        // Make sure button state is correct on scene start.
        RefreshStartBattleButton();
    }

    public void ShowDeck()
    {
        PlanningPhase();
    }

    public void HideDeck()
    {
        // Deck may not exist yet if CommonUI not loaded.
        if (deckUI != null)
            deckUI.HideDeck();
    }

    private IEnumerator InitAfterUIReady()
    {
        // Wait until CommonUI scene is loaded.
        while (!SceneManager.GetSceneByName("CommonUI").isLoaded)
            yield return null;

        // Ensure LevelsDatabase exists
        while (LevelsDatabase.Instance == null)
            yield return null;

        // Ensure GameData ready
        while (GameData.Instance == null || !GameData.Instance.IsReady)
            yield return null;

        // 1) Decide stageId: by scene name OR save currentStageId
        // If you're inside a stage scene named like "1-1" -> prefer scene name.
        string sceneStageId = SceneManager.GetActiveScene().name;
        string stageId = sceneStageId;

        // fallback: save stage
        if (string.IsNullOrEmpty(stageId) && GameData.Instance.Save != null)
            stageId = GameData.Instance.Save.currentStageId;

        // 2) Load LevelDefinition by stageId
        levelDefinition = LevelsDatabase.Instance.GetOrNull(stageId);
        if (levelDefinition == null)
            Debug.LogError($"[BattleManager] No LevelDefinition found for stageId '{stageId}'. Make sure there is an asset named '{stageId}' in Resources/Levels.");

        deckUI = FindFirstObjectByType<DeckUIController>();

        SoulsManager.instance.AddRoundSouls();
        RoundUIManager.instance.ChangeRoundText(currentRoundIndex + 1, levelDefinition.RoundsCount);
        RoundUIManager.instance.ChangeLevelText(levelDefinition.name);

        if (CameraAnimation.instance != null)
            CameraAnimation.instance.EnterGridMode();
        if (BattleFooterAnimation.instance != null)
            BattleFooterAnimation.instance.EnterGridMode();
    }


    // =======================
    // ROUND
    // =======================
    private void PlanningPhase()
    {
        Debug.Log("PlanningPhase");

        unitClassUsedThisBattle.Clear();
        unitsUsedThisBattle.Clear();

        BattleBottomPanelController.Instance.ShowDeck(PlayerDeckProvider.Instance.CurrentDeck);
        ShowDropAreasGrid();

        SetAllAIEnabled(false);


        if (deckUI != null)
            deckUI.SetCardsInteractable(true);

        if (EnemyPreviewBubblesController.Instance != null)
            EnemyPreviewBubblesController.Instance.BuildForRound(levelDefinition, currentRoundIndex);

        RemoveAllFallenWeapons();
        RemoveAllProjectiles();

        battleStarted = false;
        waitingForRoundEnd = false;

        RefreshStartBattleButton();
    }

    // =======================
    // START BATTLE
    // =======================
    public void StartRound()
    {

        if (!HasAnySpawnerPlaced())
        {
            RefreshStartBattleButton();
            return;
        }
        Debug.Log("StartRound");

        if (EnemyPreviewBubblesController.Instance != null)
            EnemyPreviewBubblesController.Instance.ClearBubbles();

        battleStarted = true;

        CameraAnimation.instance.EnterBattleMode();
        BattleFooterAnimation.instance.EnterBattleMode();

        if (deckUI != null)
            deckUI.SetCardsInteractable(false);

        var spawners = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            if (spawner != null && spawner.isActiveAndEnabled)
                spawner.StartSpawning();
        }

        RoundDefinition round = levelDefinition.rounds[currentRoundIndex];
        enemyWaveSpawner.StartWave(round.enemyPhases, round.enemyLevel);
    }

    // =======================
    // UPDATE - END BATTLE
    // =======================
    private void Update()
    {
        if (!battleStarted || gameOver)
            return;

        bool enemyAlive = AnyAlive(Team.EnemyTeam);

        if (enemyWaveSpawner.FinishedSpawning && !enemyAlive && !waitingForRoundEnd)
        {
            waitingForRoundEnd = true;
            HandleRoundWin();
        }

        if (wallHealth.destroyed)
        {
            Debug.Log("the wall is destroyed");
            HandleRoundLost();
            return;
        }
    }

    public void HandleRoundLost()
    {
        Debug.Log("The Wall is destroyed - Battle lost");

        int roundsCompleted = currentRoundIndex;
        int roundsGold = levelDefinition.GetGoldFromRounds(roundsCompleted);
        int roundsXp = levelDefinition.GetXpFromRounds(roundsCompleted);

        battleStarted = false;
        SetAllAIEnabled(false);
        gameOver = true;

        EndGameUI.Instance.ShowLoseScreen(roundsGold,roundsXp);

        foreach (var unitClass in unitClassUsedThisBattle)
            MissionsManager.Instance.ReportAction(MissionAction.PlayWithUnitClass, 1, null, unitClass);

        foreach (var unitDef in unitsUsedThisBattle)
            MissionsManager.Instance.ReportAction(MissionAction.PlayWithSpecificUnit, 1, unitDef);

        MissionsManager.Instance.ReportAction(MissionAction.PlayBattles, 1);

        PlayerCurrencyWallet.Instance.AddGold(roundsGold);
        PlayerXPManager.Instance.AddXP(roundsXp);

    }

    private void HandleRoundWin()
    {
        Debug.Log("HandleRoundWin");

        int roundsCompleted = levelDefinition.RoundsCount;

        int levelGold = levelDefinition.goldOnLevelComplete;
        int roundsGold = levelDefinition.GetGoldFromRounds(roundsCompleted);
        int totalBattleGold = levelGold + roundsGold;

        int levelXp = levelDefinition.xpOnLevelComplete;
        int roundsXp = levelDefinition.GetXpFromRounds(roundsCompleted);
        int totalXp = levelXp + roundsXp;

        if (IsExitingBattle)
            return;

        currentRoundIndex++;

        if (currentRoundIndex >= levelDefinition.RoundsCount)
        {
            foreach (var unitClass in unitClassUsedThisBattle)
                MissionsManager.Instance.ReportAction(MissionAction.PlayWithUnitClass, 1, null, unitClass);

            foreach (var unitDef in unitsUsedThisBattle)
                MissionsManager.Instance.ReportAction(MissionAction.PlayWithSpecificUnit, 1, unitDef);

            MissionsManager.Instance.ReportAction(MissionAction.WinBattles, 1);
            MissionsManager.Instance.ReportAction(MissionAction.PlayBattles, 1);

            PlayerXPManager.Instance.AddXP(levelDefinition.xpOnLevelComplete + roundsXp);
            PlayerCurrencyWallet.Instance.AddGold(levelDefinition.goldOnLevelComplete + roundsGold);

            EndGameUI.Instance.ShowWinScreen(
                levelGold,
                roundsGold,
                totalBattleGold,
                levelXp,
                roundsXp,
                totalXp,
                levelDefinition.name
            );

            // --- SAVE PROGRESS (stage complete) ---
            if (GameData.Instance != null && GameData.Instance.Save != null && LevelsDatabase.Instance != null)
            {
                string stageId = UnityEngine.SceneManagement.SceneManager.GetActiveScene().name;

                if (!GameData.Instance.Save.completedStageIds.Contains(stageId))
                    GameData.Instance.Save.completedStageIds.Add(stageId);

                // advance to next stage
                string next = LevelsDatabase.Instance.GetNextStageId(stageId);
                if (!string.IsNullOrEmpty(next))
                    GameData.Instance.Save.currentStageId = next;

                GameData.Instance.SaveNow();
            }

            gameOver = true;
            return;
        }

        SoulsManager.instance.AddRoundSouls();

        RoundUIManager.instance.ChangeRoundText(currentRoundIndex + 1, levelDefinition.RoundsCount);

        var spawners = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);
        foreach (var s in spawners)
            s.ResetForNewRound();

        ClearPreviousRoundUnits();

        if (WaveMessageUI.Instance != null)
            WaveMessageUI.Instance.ShowMessage($"YOU WON WAVE {currentRoundIndex}!", 2f);

        StartCoroutine(StartNextWaveAfterDelay());

        if (CameraAnimation.instance != null)
            CameraAnimation.instance.EnterGridMode();

        if (BattleFooterAnimation.instance != null)
            BattleFooterAnimation.instance.EnterGridMode();
    }

    // =======================
    // SPAWNER DETECTION
    // =======================
    public bool HasAnySpawnerPlaced()
    {
        var spawners = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);

        for (int i = 0; i < spawners.Length; i++)
        {
            if (spawners[i] != null)
                return true;
        }
        return false;
    }


    public void RefreshStartBattleButton()
    {
        if (StartBattleButton.instance == null)
            return;

        bool canStart = !battleStarted && !gameOver && HasAnySpawnerPlaced();

        if (canStart)
            StartBattleButton.instance.EnableButton();
        else
            StartBattleButton.instance.DisableButton();
    }

    // =======================
    // HELPERS
    // =======================
    public bool AnyAlive(Team team)
    {
        var units = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        foreach (var u in units)
            if (u.team == team && u.currentHealth > 0)
                return true;
        return false;
    }

    private void SetAllAIEnabled(bool enabled)
    {
        var allAI = FindObjectsByType<UnitAI>(FindObjectsSortMode.None);
        foreach (var ai in allAI)
            ai.enabled = enabled;
    }

    private void HideDropAreasGrid()
    {
        foreach (var grid in dropAreaGrids)
            grid.gameObject.SetActive(false);
    }

    private void ShowDropAreasGrid()
    {
        foreach (var grid in dropAreaGrids)
            grid.gameObject.SetActive(true);
    }

    private void RemoveAllFallenWeapons()
    {
        foreach (var w in GameObject.FindGameObjectsWithTag("FallenWeapon"))
            Destroy(w);
    }

    private void RemoveAllProjectiles()
    {
        foreach (var p in GameObject.FindGameObjectsWithTag("Projectile"))
            Destroy(p);
    }

    private IEnumerator StartNextWaveAfterDelay()
    {
        yield return new WaitForSeconds(2.5f);
        PlanningPhase();
    }

    public void RegisterUnitClassUsed(UnitClass unitClass)
    {
        unitClassUsedThisBattle.Add(unitClass);
    }

    public void RegisterUnitUsed(UnitDefinition unitDef)
    {
        unitsUsedThisBattle.Add(unitDef);
    }

    private void ClearPreviousRoundUnits()
    {
        var units = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        foreach (var u in units)
            Destroy(u.gameObject);
    }

    public void ExitBattle()
    {
        IsExitingBattle = true;
    }
}
