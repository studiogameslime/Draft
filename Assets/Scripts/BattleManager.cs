using System.Collections;
using UnityEngine;

public class BattleManager : MonoBehaviour
{
    [Header("Grids")]
    public MonsterGrid myGrid;
    public MonsterGrid enemyGrid;

    [Header("Selection UI")]
    public UnitSelectionUI selectionUI;
    public int picksToDo = 3;
    public float startBattleDelay = 3f;

    private int picksDone = 0;
    private bool battleStarted = false;

    private void Start()
    {
        SetAllAIEnabled(false);

        if (selectionUI != null)
        {
            selectionUI.battleManager = this;
            selectionUI.RollNewUnits();  
        }
    }

    public void OnPlayerPickedUnit(UnitDefinition def)
    {
        if (battleStarted) return;
        if (def == null || def.prefab == null || myGrid == null) return;

        for (int i = 0; i < def.spawnCount; i++)
        {
            myGrid.AddMonster(def, Team.MyTeam);
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

    private void StartBattle()
    {
        battleStarted = true;

        var allStats = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        foreach (var s in allStats)
        {
            s.lockedIn = true;
        }

        SetAllAIEnabled(true);
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
}
