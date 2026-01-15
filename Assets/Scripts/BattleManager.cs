using System;
using System.Collections;
using System.Collections.Generic;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    [Header("Grids")]
    public MonsterGrid myGrid;

    [Header("Selection UI")]
    public UnitSelectionUI selectionUI;

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

    [HideInInspector] public int currentRoundIndex = 0;

    private HashSet<UnitClass> unitClassUsedThisBattle = new();
    private HashSet<UnitDefinition> unitsUsedThisBattle = new();

    public bool IsBattleRunning => battleStarted;
    public bool IsGameOver => gameOver;

    public WallHealth wallHealth;

    // =======================
    // START
    // =======================
    private void Start()
    {
        StartCoroutine(InitAfterUIReady());
        Initialize();
        CameraAnimation.instance.EnterGridMode();
    }

    public void Initialize()
    {
        SetAllAIEnabled(false);

        enemyWaveSpawner = FindFirstObjectByType<EnemyWaveSpawner>();
        dropAreaGrids = FindObjectsByType<DropAreaGrid>(FindObjectsSortMode.None);

        // Make sure button state is correct on scene start
        RefreshStartBattleButton();
    }

    public void ShowDeck()
    {
        PlanningPhase();

    }

    public void HideDeck()
    {
        deckUI.HideDeck();
    }


    private IEnumerator InitAfterUIReady()
    {
        while (!SceneManager.GetSceneByName("CommonUI").isLoaded)
            yield return null;

        selectionUI = FindFirstObjectByType<UnitSelectionUI>();
        deckUI = FindFirstObjectByType<DeckUIController>();


        while (selectionUI == null)
        {
            selectionUI = FindFirstObjectByType<UnitSelectionUI>();
            deckUI = FindFirstObjectByType<DeckUIController>();
            yield return null;
        }
        SoulsManager.instance.AddRoundSouls();
        RoundUIManager.instance.ChangeRoundText(currentRoundIndex + 1, levelDefinition.RoundsCount);
    }

    // =======================
    // ROUND
    // =======================
    private void PlanningPhase()
    {
        Debug.Log("PlanningPhase");

        unitClassUsedThisBattle.Clear();
        unitsUsedThisBattle.Clear();
        deckUI.ShowDeck();
        ShowDropAreasGrid();

        

        SetAllAIEnabled(false);

        if (selectionUI != null)
        {
            selectionUI.gameObject.SetActive(true);
            selectionUI.battleManager = this;
            selectionUI.RollNewUnits();
        }

        if (deckUI != null)
            deckUI.SetCardsInteractable(true);
        if(EnemyPreviewBubblesController.Instance != null)
            EnemyPreviewBubblesController.Instance.BuildForRound(levelDefinition, currentRoundIndex);

        RemoveAllFallenWeapons();
        RemoveAllProjectiles();

        battleStarted = false;
        waitingForRoundEnd = false;

        // Button depends on whether at least 1 UnitSpawner is placed
        RefreshStartBattleButton();
    }

    // =======================
    // START BATTLE
    // =======================
    public void StartRound()
    {
        Debug.Log("StartRound");
        // Do not start battle if there are no spawners placed
        if (!HasAnySpawnerPlaced())
        {
            RefreshStartBattleButton();
            return;
        }

        if (EnemyPreviewBubblesController.Instance != null)
            EnemyPreviewBubblesController.Instance.ClearBubbles();

        battleStarted = true;
        CameraAnimation.instance.EnterBattleMode();


        if (deckUI != null)
            deckUI.SetCardsInteractable(false);

       // HideDropAreasGrid();

        // Start ALL UnitSpawners (one per cell)
        var spawners = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);
        foreach (var spawner in spawners)
        {
            if (spawner != null && spawner.isActiveAndEnabled)
                spawner.StartSpawning(this);
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

        bool myAlive = AnyAlive(Team.MyTeam);
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

        // ============================================
        // MISSIONS PROGRESS
        //=============================================

        foreach (var unitClass in unitClassUsedThisBattle)
        {
            MissionsManager.Instance.ReportAction(
                MissionAction.PlayWithUnitClass,
                1,
                null,
                unitClass
            );
        }
        foreach (var unitDef in unitsUsedThisBattle)
        {
            MissionsManager.Instance.ReportAction(
                MissionAction.PlayWithSpecificUnit,
                1,
                unitDef
            );
        }

        MissionsManager.Instance.ReportAction(MissionAction.PlayBattles, 1);

        // ============================================

    }
    private void HandleRoundWin()
    {
        Debug.Log("HandleRoundWin");

        currentRoundIndex++;
        if (currentRoundIndex >= levelDefinition.RoundsCount) // Win condition
        {
            PlayerXPManager.Instance.AddXP(levelDefinition.xpOnLevelComplete);

            // ============================================
            // MISSIONS PROGRESS
            //=============================================

            foreach (var unitClass in unitClassUsedThisBattle)
            {
                MissionsManager.Instance.ReportAction(
                    MissionAction.PlayWithUnitClass,
                    1,
                    null,
                    unitClass
                );
            }
            foreach (var unitDef in unitsUsedThisBattle)
            {
                MissionsManager.Instance.ReportAction(
                    MissionAction.PlayWithSpecificUnit,
                    1,
                    unitDef
                );
            }
            MissionsManager.Instance.ReportAction(MissionAction.WinBattles, 1);
            MissionsManager.Instance.ReportAction(MissionAction.PlayBattles, 1);

            // ============================================

            EndGameUI.Instance.ShowWinScreen(
                levelDefinition.goldOnLevelComplete,
                levelDefinition.GetGoldFromRounds(),
                PlayerCurrencyWallet.Instance.Gold
            );

            gameOver = true;
            return;
        }

        SoulsManager.instance.AddRoundSouls();
        RoundUIManager.instance.ChangeRoundText(currentRoundIndex + 1, levelDefinition.RoundsCount);

        var round = levelDefinition.rounds[currentRoundIndex];
        PlayerXPManager.Instance.AddXP(round.xpOnRoundWin);


        var spawners = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);
        foreach (var s in spawners)
            s.ResetForNewRound();

        ClearPreviousRoundUnits();

        WaveMessageUI.Instance.ShowMessage($"YOU WON WAVE {currentRoundIndex}!", 2f);
        StartCoroutine(StartNextWaveAfterDelay());
        CameraAnimation.instance.EnterGridMode();
    }

    // =======================
    // SPAWNER DETECTION (NEW)
    // =======================

    /// <summary>
    /// True if at least one UnitSpawner exists on the board during planning phase.
    /// </summary>
    public bool HasAnySpawnerPlaced()
    {
        var spawners = FindObjectsByType<UnitSpawner>(FindObjectsSortMode.None);
        return spawners != null && spawners.Length > 0;
    }

    /// <summary>
    /// Call this after placing/removing a spawner to update the Start Battle button.
    /// </summary>
    public void RefreshStartBattleButton()
    {
        if (StartBattleButton.instance == null)
            return;

        bool canStart = !battleStarted && !gameOver && HasAnySpawnerPlaced();
        Debug.Log($"can start {canStart}");
        // Use whatever API you already have on StartBattleButton:
        // If you only have EnableButton(), keep that + add a DisableButton() in that class.
        if (canStart)
            StartBattleButton.instance.EnableButton();
        else
        {
            // If you don't have DisableButton(), add it (recommended)
            // or set interactable = false internally.
            StartBattleButton.instance.DisableButton();
        }
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
        {
                Destroy(u.gameObject);
        }
    }

}
