using System.Collections;
using UnityEngine;

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

    // Internal state
    private int currentRoundIndex = 0;
    private int picksDone = 0;
    private int picksToDo = 3;

    private bool battleStarted = false;
    private bool gameOver = false;

    // ============================================================
    // START
    // ============================================================
    private void Start()
    {
        SetAllAIEnabled(false);

        if (levelDefinition == null || levelDefinition.RoundsCount == 0)
        {
            Debug.LogError("BattleManager: LevelDefinition missing or empty.");
            return;
        }

        StartRound(0);
    }

    // ============================================================
    // ROUND START
    // ============================================================
    private void StartRound(int index)
    {
        Debug.Log($"--- ROUND {index + 1}/{levelDefinition.RoundsCount} START ---");

        currentRoundIndex = index;
        RoundDefinition round = levelDefinition.rounds[currentRoundIndex];

        // ודאות שאין AI רץ בזמן ההחזרה
        SetAllAIEnabled(false);

        // 1) מחזירים את כל היחידות לחיים ומחזירים לעמדות מסודרות
        ResetUnitsForNewRound();

        // 2) איפוס בחירות
        picksDone = 0;
        picksToDo = Mathf.Max(1, round.playerPicks);

        // 3) ספאון גל האויב של הסיבוב הזה
        SpawnEnemyWave(round);

        // 4) פתיחת UI בחירה
        if (selectionUI != null)
        {
            selectionUI.gameObject.SetActive(true);
            selectionUI.battleManager = this;
            selectionUI.RollNewUnits();
        }

        battleStarted = false;
    }

    /// <summary>
    /// מחזיר יחידות לחיים, משחרר נעילות, מאפס מהירות ומסדר אותן מחדש בגריד.
    /// </summary>
    private void ResetUnitsForNewRound()
    {
        var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        foreach (var u in all)
        {
            // revive stats + HP bar + animation state
            u.Revive();

            // allow grid to move them back to formation
            u.lockedIn = false;

            // stop any residual movement
            var rb = u.GetComponent<Rigidbody2D>();
            if (rb != null)
                rb.linearVelocity = Vector2.zero;

            var anim = u.GetComponent<Animator>();
            if (anim != null)
                anim.SetBool("isMoving", false);
        }

        // re-arrange both grids so everyone חוזר ל"עמדות המקוריות" בגריד
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
        if (enemyGrid == null) return;

        if (round.enemySpawns == null) return;

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

    // ============================================================
    // PLAYER PICKS
    // ============================================================
    public void OnPlayerPickedUnit(UnitDefinition def)
    {
        if (battleStarted || gameOver) return;
        if (def == null || myGrid == null) return;

        for (int i = 0; i < def.spawnCount; i++)
        {
            myGrid.AddMonster(def, Team.MyTeam, playerUnitsLevel);
        }

        picksDone++;

        if (picksDone >= picksToDo)
        {
            if (selectionUI != null)
                selectionUI.gameObject.SetActive(false);

            StartCoroutine(StartBattleAfterDelay());
        }
        else
        {
            if (selectionUI != null)
                selectionUI.RollNewUnits();
        }
    }

    private IEnumerator StartBattleAfterDelay()
    {
        yield return new WaitForSeconds(startBattleDelay);
        StartBattle();
    }

    // ============================================================
    // BATTLE START
    // ============================================================
    private void StartBattle()
    {
        battleStarted = true;

        LockAllUnits();
        SetAllAIEnabled(true);
    }

    // ============================================================
    // UPDATE — CHECK FOR ROUND END
    // ============================================================
    private void Update()
    {
        if (!battleStarted || gameOver) return;

        bool myAlive = AnyAlive(Team.MyTeam);
        bool enemyAlive = AnyAlive(Team.EnemyTeam);

        if (!myAlive || !enemyAlive)
        {
            EndBattle(myAlive, enemyAlive);
        }
    }

    private void EndBattle(bool myAlive, bool enemyAlive)
    {
        battleStarted = false;
        SetAllAIEnabled(false);

        if (!myAlive)
        {
            Debug.Log("Player lost — stage failed.");
            gameOver = true;
            // TODO: show lose screen
            return;
        }

        if (!enemyAlive)
        {
            HandleRoundWin();
        }
    }

    // ============================================================
    // ROUND WON
    // ============================================================
    private void HandleRoundWin()
    {
        Debug.Log($"ROUND {currentRoundIndex + 1} WON!");

        currentRoundIndex++;

        if (currentRoundIndex >= levelDefinition.RoundsCount)
        {
            Debug.Log("PLAYER WON THE ENTIRE LEVEL!");
            gameOver = true;
            // TODO: show win screen
            return;
        }

        // סיבוב חדש
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
}
