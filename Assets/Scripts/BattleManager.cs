using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

public class BattleManager : MonoBehaviour
{
    [Header("Grids")]
    public MonsterGrid myGrid;
    public MonsterGrid enemyGrid;

    [Header("Selection UI")]
    public UnitSelectionUI selectionUI;

    [Header("Level Data")]
    public LevelDefinition levelDefinition;

    [Header("Player unit level (from meta progression)")]
    public int playerUnitsLevel = 1;

    [Header("Timing")]
    public float startBattleDelay = 2f;

    [Header("Deck UI Manager (for enabling/disabling cards)")]
    public DeckUIController deckUI;

    public DropAreaGrid[] dropAreaGrids;

    // Internal state
    [HideInInspector] public int currentRoundIndex = 0;
    private int picksDone = 0;
    private int picksToDo = 3;
    private bool battleStarted = false;
    private bool gameOver = false;

    // ============================================================
    // PUBLIC ACCESSORS
    // ============================================================
    public bool IsBattleRunning => battleStarted;

    // ============================================================
    // START
    // ============================================================
    private void Start()
    {
        {
            Scene uiScene = SceneManager.GetSceneByName("CommonUI");
            if (uiScene.isLoaded)
            {
                AssignUIReferences();
            }
            else
            {
               StartCoroutine(WaitForUIAndAssign());
            }
        }


        SetAllAIEnabled(false);

        if (levelDefinition == null || levelDefinition.RoundsCount == 0)
        {
            Debug.LogError("BattleManager: LevelDefinition missing or empty.");
            return;
        }
        
        StartRound(0);

        dropAreaGrids = FindObjectsByType<DropAreaGrid>(FindObjectsSortMode.None);


    }

    // ============================================================
    // Load selectionUI to script
    // ============================================================
    private IEnumerator WaitForUIAndAssign()
    {
        while (!SceneManager.GetSceneByName("CommonUI").isLoaded)
            yield return null;

        AssignUIReferences();
    }

    private void AssignUIReferences()
    {
        if (selectionUI == null)
        { 
            selectionUI = FindFirstObjectByType<UnitSelectionUI>();
            deckUI = FindFirstObjectByType<DeckUIController>();
        }

        if (selectionUI == null)
        {
            Debug.LogError("BattleManager: UnitSelectionUI not found in CommonUI!");
        }
        else
        {
            Debug.Log("BattleManager: SelectionUI connected automatically");
        }
    }

    // ============================================================
    // ROUND START
    // ============================================================
    private void StartRound(int index)
    {
        ShowDropAreasGrid();
        StartBattleButton.instance.EnableButton();
        Debug.Log($"--- ROUND {index + 1}/{levelDefinition.RoundsCount} START ---");
        SoulsManager.instance.AddRoundSouls();
        currentRoundIndex = index;
        RoundDefinition round = levelDefinition.rounds[currentRoundIndex];

        // Turn off AI and reset board
        SetAllAIEnabled(false);

        // 1) Reset units
        ResetUnitsForNewRound();

        // 2) Reset picks
        picksDone = 0;
        picksToDo = Mathf.Max(1, round.playerPicks);

        // 3) Spawn enemy wave
        SpawnEnemyWave(round);

        // 4) Show UI roll
        if (selectionUI != null)
        {
            selectionUI.gameObject.SetActive(true);
            selectionUI.battleManager = this;
            selectionUI.RollNewUnits();
            Debug.Log("Rolled Units!");
        }

        // 5) RE-ENABLE DECK (planning phase UI)
        if (deckUI != null)
            deckUI.SetCardsInteractable(true);

        battleStarted = false;
    }

    /// <summary>
    /// Revive and reset all units, prepare both grids for a clean new round.
    /// </summary>
    private void ResetUnitsForNewRound()
    {
        var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        foreach (var u in all)
        {
            u.Revive();
            u.lockedIn = false;

            var rb = u.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            var anim = u.GetComponent<Animator>();
            if (anim != null)
            {
                anim.ResetTrigger("dying");
                anim.ResetTrigger("attack");
                anim.SetBool("isMoving", false);
                anim.Rebind();
                anim.Update(0f);
            }
        }

        if (myGrid != null)
            myGrid.ArrangeMonsters();

        if (enemyGrid != null)
            enemyGrid.ArrangeMonsters();
    }

    // ============================================================
    // ENEMY SPAWN
    // ============================================================
    private void SpawnEnemyWave(RoundDefinition round)
    {
        if (enemyGrid == null || round.enemySpawns == null)
            return;

        foreach (var entry in round.enemySpawns)
        {
            if (entry == null || entry.unit == null) continue;

            for (int i = 0; i < entry.count; i++)
            {
                enemyGrid.AddMonster(entry.unit, Team.EnemyTeam, entry.level);
            }
        }

        enemyGrid.ArrangeMonsters();
    }

    /// <summary>
    /// This version is used when player places units manually via drag & drop deck.
    /// </summary>
    public void StartBattleFromDeck()
    {
        // Enable AI for all alive units
        var allUnits = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        foreach (var u in allUnits)
        {
            if (u == null || u.currentHealth <= 0)
                continue;

            var ai = u.GetComponent<EnemyAI>();
            if (ai != null)
                ai.enabled = true;
        }

        LockAllUnits();
        battleStarted = true;

        // Disable deck UI
        if (deckUI != null)
            deckUI.SetCardsInteractable(false);
        HideDropAreasGrid();

    }

    // ============================================================
    // UPDATE — CHECK END OF ROUND
    // ============================================================
    private void Update()
    {
        if (!battleStarted || gameOver) return;

        bool myAlive = AnyAlive(Team.MyTeam);
        bool enemyAlive = AnyAlive(Team.EnemyTeam);

        if (!myAlive || !enemyAlive)
            EndBattle(myAlive, enemyAlive);
    }

    private void EndBattle(bool myAlive, bool enemyAlive)
    {
        battleStarted = false;
        SetAllAIEnabled(false);

        if (!myAlive)
        {
            Debug.Log("Player lost — stage failed.");
            gameOver = true;
            return;
        }

        if (!enemyAlive)
            HandleRoundWin();
    }

    // ============================================================
    // ROUND WIN
    // ============================================================
    private void HandleRoundWin()
    {
        Debug.Log($"ROUND {currentRoundIndex + 1} WON!");
        currentRoundIndex++;

        if (currentRoundIndex >= levelDefinition.RoundsCount)
        {
            Debug.Log("PLAYER WON THE ENTIRE LEVEL!");

            var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
            foreach (var u in all)
                u.Winning();

            gameOver = true;
            return;
        }

        StartRound(currentRoundIndex);
    }

    // ============================================================
    // HELPERS
    // ============================================================
    private bool AnyAlive(Team team)
    {
        var units = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        foreach (var u in units)
        {
            if (u.team == team && u.currentHealth > 0)
                return true;
        }

        return false;
    }

    private void SetAllAIEnabled(bool enabled)
    {
        var allAI = FindObjectsByType<EnemyAI>(FindObjectsSortMode.None);

        foreach (var ai in allAI)
        {
            ai.enabled = enabled;

            if (!enabled)
            {
                var anim = ai.GetComponent<Animator>();
                if (anim != null)
                    anim.SetBool("isMoving", false);
            }
        }
    }

    private void LockAllUnits()
    {
        var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        foreach (var u in all)
            u.lockedIn = true;
    }

    private void UnlockAllUnits()
    {
        var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        foreach (var u in all)
            u.lockedIn = false;
    }

    public void HideDropAreasGrid()
    {
        foreach (var grid in dropAreaGrids)
        {
            Debug.Log(grid.name);
            grid.gameObject.SetActive(false);
        }
    }

    public void ShowDropAreasGrid()
    {
        foreach (var grid in dropAreaGrids)
        {
            grid.gameObject.SetActive(true);
        }
    }
}
