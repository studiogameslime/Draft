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

        deckUI = FindFirstObjectByType<DeckUIController>();
            
        SoulsManager.instance.AddRoundSouls();
        RoundUIManager.instance.ChangeRoundText(currentRoundIndex + 1, levelDefinition.RoundsCount);

        if (CameraAnimation.instance != null)
            CameraAnimation.instance.EnterGridMode();

        if (BattleFooterAnimation.instance != null)
        {
            Debug.Log("BattleFooter");
            BattleFooterAnimation.instance.EnterGridMode();
        }
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

    private void HandleRoundLost()
    {
        Debug.Log("The Wall is destroyed - Battle lost");

        battleStarted = false;
        SetAllAIEnabled(false);
        gameOver = true;

        EndGameUI.Instance.ShowLoseScreen();

        foreach (var unitClass in unitClassUsedThisBattle)
            MissionsManager.Instance.ReportAction(MissionAction.PlayWithUnitClass, 1, null, unitClass);

        foreach (var unitDef in unitsUsedThisBattle)
            MissionsManager.Instance.ReportAction(MissionAction.PlayWithSpecificUnit, 1, unitDef);

        MissionsManager.Instance.ReportAction(MissionAction.PlayBattles, 1);
    }

    private void HandleRoundWin()
    {
        int levelGold = levelDefinition.goldOnLevelComplete;
        int roundsGold = levelDefinition.GetGoldFromRounds();
        int totalBattleGold = levelGold + roundsGold;

        if (IsExitingBattle)
            return;

        Debug.Log("HandleRoundWin");

        currentRoundIndex++;

        if (currentRoundIndex >= levelDefinition.RoundsCount)
        {
            foreach (var unitClass in unitClassUsedThisBattle)
                MissionsManager.Instance.ReportAction(MissionAction.PlayWithUnitClass, 1, null, unitClass);

            foreach (var unitDef in unitsUsedThisBattle)
                MissionsManager.Instance.ReportAction(MissionAction.PlayWithSpecificUnit, 1, unitDef);

            MissionsManager.Instance.ReportAction(MissionAction.WinBattles, 1);
            MissionsManager.Instance.ReportAction(MissionAction.PlayBattles, 1);

            PlayerXPManager.Instance.AddXP(levelDefinition.xpOnLevelComplete);
            PlayerCurrencyWallet.Instance.AddGold(levelDefinition.goldOnLevelComplete);
            PlayerCurrencyWallet.Instance.AddGold(levelDefinition.GetGoldFromRounds());

            EndGameUI.Instance.ShowWinScreen(
                levelGold,
                roundsGold,
                totalBattleGold


            );
            


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
