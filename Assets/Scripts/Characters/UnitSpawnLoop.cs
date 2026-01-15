using System.Collections;
using UnityEngine;

public class UnitSpawnLoop : MonoBehaviour
{
    private BattleManager battle;
    private UnitDefinition def;
    private Team team;
    private int level;
    private Vector3 slotWorldPos;
    private Vector3 targetScale;
    private Transform parentForSpawns;

    private bool running;

    public void Init(
        BattleManager battle,
        UnitDefinition def,
        Team team,
        int level,
        Vector3 slotWorldPos,
        Vector3 targetScale,
        Transform parentForSpawns)
    {
        this.battle = battle;
        this.def = def;
        this.team = team;
        this.level = level;
        this.slotWorldPos = slotWorldPos;
        this.targetScale = targetScale;
        this.parentForSpawns = parentForSpawns;
    }

    public void StartLoop()
    {
        if (running) return;
        running = true;
        StartCoroutine(Loop());
    }

    private IEnumerator Loop()
    {
        while (battle != null && battle.IsBattleRunning && !battle.IsGameOver)
        {
            while (def != null && !UnitCapacityManager.Instance.CanSpawn(def))
                yield return null;

            CharacterStats next = SpawnWaitingUnit();
            if (next == null) yield break;

            UnitCapacityManager.Instance.RegisterSpawn(def);

            float duration = Mathf.Max(0.01f, def.spawnTime);
            float t = 0f;

            while (t < duration && battle.IsBattleRunning && !battle.IsGameOver)
            {
                t += Time.deltaTime;
                float a = Mathf.Clamp01(t / duration);
                next.transform.localScale = Vector3.Lerp(Vector3.zero, targetScale, a);
                yield return null;
            }

            if (!battle.IsBattleRunning || battle.IsGameOver)
                yield break;

            EnableCombat(next);
            yield return null;
        }
    }

    private CharacterStats SpawnWaitingUnit()
    {
        if (def == null || def.prefab == null) return null;

        GameObject go = Instantiate(def.prefab, parentForSpawns);
        go.transform.position = slotWorldPos;
        go.transform.rotation = Quaternion.identity;
        go.transform.localScale = Vector3.zero;

        var stats = go.GetComponent<CharacterStats>();
        if (stats != null)
        {
            stats.Init(team, def, level);
            stats.lockedIn = true;
            stats._initialPosition = slotWorldPos;
        }

        var ai = go.GetComponent<UnitAI>();
        if (ai != null) ai.enabled = false;

        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        return stats;
    }

    private void EnableCombat(CharacterStats stats)
    {
        if (stats == null) return;

        stats.lockedIn = true;

        var ai = stats.GetComponent<UnitAI>();
        if (ai != null)
        {
            ai.enabled = true;
            ai.LockInitialTargetAtBattleStart();
        }

        var ninja = stats.GetComponent<NinjaStealthRun>();
        if (ninja != null)
            ninja.DoInitialStealthRun();
    }
}
