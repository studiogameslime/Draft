using System.Collections;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Lives on a grid cell spawner object.
/// Responsible for spawning combat units based on UnitDefinition.
/// One spawner = one cell = independent capacity.
///
/// Rules:
/// - Capacity counts only active fighters.
/// - This spawner also keeps one reserve unit waiting on the cell (not counted in capacity).
/// - Reserve is created after spawnTime (no scale animation).
/// - While spawning, we update only the progress UI.
/// - When there is free attacker capacity, reserve is released immediately to battle.
/// </summary>
public class UnitSpawner : MonoBehaviour
{
    [Header("Setup")]
    public UnitDefinition unitDef;
    public Team team = Team.MyTeam;
    public int unitLevel = 1;

    [Header("Optional visuals (for planning phase)")]
    [SerializeField] private SpriteRenderer iconRenderer;

    [Header("Spawn Progress UI (optional)")]
    private Image spawnProgressFill; // Set this to your radial/filled Image
    [SerializeField] private float progressLerpSpeed = 12f; // Smooth UI follow speed

    [Header("Runtime")]
    private bool battleRunning = false;
    private int attackersActive = 0;
    private CharacterStats reserveStats = null;
    private bool reserveReady = false;
    private bool isSpawningReserve = false;
    private float hpMul = 1f;
    private float dmgMul = 1f;
    private float spawnTimeMul = 1f;

    // UI smoothing runtime
    private float progressTarget = 0f;
    private Coroutine progressRoutine;
    private Transform unitsContainer;
    // Inside fields section
    private Color defaultProgressColor; // Stores the original bar color [1]
    private bool spawnedStaticUnit = false;

    public bool HasReserveWaiting => reserveReady && reserveStats != null;

    private void Awake()
    {
        unitsContainer = GameObject.FindGameObjectWithTag("UnitsContainer").transform;

        if (spawnProgressFill != null)
        {
            defaultProgressColor = spawnProgressFill.color;
        }
    }

    public void Configure(UnitDefinition def, Team t, int level)
    {
        unitDef = def;
        team = t;
        unitLevel = level;

        if (iconRenderer != null && def != null)
        {
            iconRenderer.sprite = def.icon;
            if (def.prefab != null)
                iconRenderer.transform.localScale = def.prefab.transform.localScale;
        }
    }

    public void SetCellBonusMultipliers(float hpMultiplier, float dmgMultiplier, float spawnMultiplier)
    {
        hpMul = Mathf.Max(0.01f, hpMultiplier);
        dmgMul = Mathf.Max(0.01f, dmgMultiplier);
        spawnTimeMul = Mathf.Max(0.01f, spawnMultiplier);
    }

    public void StartSpawning()
    {
        if (battleRunning) return;

        battleRunning = true;
        SetSpawnerAlpha(0.5f);

        // Reset progress at battle start
        SetProgressImmediate(0f);

        StopAllCoroutines();
        StartCoroutine(BrainLoop());
    }

    private IEnumerator BrainLoop()
    {
        if (unitDef == null || unitDef.prefab == null)
            yield break;

        // NEW: Spawn exactly one unit immediately for units that disable reserve
        if (unitDef.disableReserveUnit && !spawnedStaticUnit)
        {
            SpawnSingleUnitImmediate();
            spawnedStaticUnit = true;
        }

        while (battleRunning && BattleManager.instance != null &&
               BattleManager.instance.IsBattleRunning && !BattleManager.instance.IsGameOver)
        {
            int cap = Mathf.Max(1, unitDef.baseCapacity);

            if (!unitDef.disableReserveUnit)
            {
                if (!reserveReady && !isSpawningReserve)
                    StartCoroutine(SpawnReserveRoutine());

                if (reserveReady && attackersActive < cap)
                    ReleaseReserveToBattle();
            }

            yield return null;
        }
    }
    private void SpawnSingleUnitImmediate()
    {
        GameObject go = Instantiate(unitDef.prefab, transform.position, Quaternion.identity);

        // Ensure owner link exists and points to this spawner BEFORE skill init
        var link = go.GetComponent<SpawnedUnitOwnerLink>();
        if (link == null)
            link = go.AddComponent<SpawnedUnitOwnerLink>();
        link.owner = this;

        var stats = go.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError("Spawned prefab has no CharacterStats.");
            Destroy(go);
            return;
        }

        stats.Init(team, unitDef, unitLevel);

        // Init built-in skills on prefab (MinerBehaviour will now find link.owner)
        InitExistingSkillBehaviours(go, stats);

        ApplyUpgradesToSpawnedUnit(go, stats);
        stats.SetInitialPosition();
        go.transform.SetParent(unitsContainer, true);

        // Apply cell bonus
        stats.maxHealth = Mathf.RoundToInt(stats.maxHealth * hpMul);
        stats.currentHealth = stats.maxHealth;
        stats.damage = Mathf.RoundToInt(stats.damage * dmgMul);

        stats.isUntargetable = true;

        SetReserveCollision(go, false);

        attackersActive = Mathf.Max(attackersActive, 1);
    }

    private IEnumerator SpawnReserveRoutine()
    {
        if (isSpawningReserve) yield break;
        isSpawningReserve = true;

        if (unitDef == null || unitDef.prefab == null)
        {
            isSpawningReserve = false;
            yield break;
        }

        if (reserveReady || reserveStats != null)
        {
            isSpawningReserve = false;
            yield break;
        }

        // Start progress from 0 when reserve spawn begins
        SetProgressImmediate(0f);

        // Wait spawnTime while updating only the progress UI (no unit instantiation, no scale animation)
        //float levelMul = UnitLevelingService.GetSpawnTimeMultiplier(unitLevel);
        //float duration = Mathf.Max(0.01f, unitDef.spawnTime * levelMul);
        float duration = unitDef.spawnTime * spawnTimeMul;
        float t = 0f;

        while (t < duration && battleRunning && BattleManager.instance != null && BattleManager.instance.IsBattleRunning && !BattleManager.instance.IsGameOver)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / duration);
            SetProgressTarget(a);
            yield return null;
        }

        // Battle ended mid-spawn
        if (!battleRunning || BattleManager.instance == null || !BattleManager.instance.IsBattleRunning || BattleManager.instance.IsGameOver)
        {
            isSpawningReserve = false;
            yield break;
        }

        // Ensure progress ends at 1 when reserve is ready
        SetProgressTarget(1f);

        // Now create the reserve unit at its normal scale
        GameObject go = Instantiate(unitDef.prefab, transform.position, Quaternion.identity);
        var link = go.GetComponent<SpawnedUnitOwnerLink>();
        if (link == null)
            link = go.AddComponent<SpawnedUnitOwnerLink>();
        link.owner = this;
        var stats = go.GetComponent<CharacterStats>();
        if (stats == null)
        {
            Debug.LogError("Spawned prefab has no CharacterStats.");
            Destroy(go);
            isSpawningReserve = false;
            yield break;
        }

        // Init stats
        stats.Init(team, unitDef, unitLevel);
        InitExistingSkillBehaviours(go, stats);

        ApplyUpgradesToSpawnedUnit(go, stats);
        stats.SetInitialPosition();
        go.transform.SetParent(unitsContainer, true);

        // Apply cell bonus
        stats.maxHealth = Mathf.RoundToInt(stats.maxHealth * hpMul);
        stats.currentHealth = stats.maxHealth;
        stats.damage = Mathf.RoundToInt(stats.damage * dmgMul);

        // Reserve behavior: cannot be targeted, AI off
        stats.isUntargetable = true;

        SetReserveBrain(go, false);


        SetReserveCollision(go, false);

        // Keep reserve from drifting while waiting
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        reserveStats = stats;
        reserveReady = true;
        isSpawningReserve = false;
    }

    private void ReleaseReserveToBattle()
    {
        if (!reserveReady || reserveStats == null)
            return;

        // Make the reserve targetable now
        reserveStats.isUntargetable = false;
        SetReserveCollision(reserveStats.gameObject, true);
        // Ensure this released unit will notify us when it dies (capacity tracking)
        var link = reserveStats.GetComponent<SpawnedUnitOwnerLink>();
        if (link == null)
            link = reserveStats.gameObject.AddComponent<SpawnedUnitOwnerLink>();
        link.owner = this;

        SetReserveBrain(reserveStats.gameObject, true);

        var ninja = reserveStats.GetComponent<NinjaStealthRun>();
        if (ninja != null)
            ninja.DoInitialStealthRun();

        attackersActive++;
        reserveStats = null;
        reserveReady = false;

        // After releasing, we will start a new reserve spawn, so reset progress
        SetProgressTarget(0f);
    }

    private void SetReserveBrain(GameObject go, bool enabled)
    {
        if (go == null) return;

        // Enemy/normal AI
        var unitAI = go.GetComponent<UnitAI>();
        if (unitAI != null)
        {
            unitAI.enabled = enabled;
            if (enabled)
                unitAI.LockInitialTargetAtBattleStart();
        }

        // Fairy AI
        var fairyAI = go.GetComponent<FairyAI>();
        if (fairyAI != null)
            fairyAI.enabled = enabled;
    }

    public void NotifyUnitDied()
    {
        attackersActive = Mathf.Max(0, attackersActive - 1);
        int cap = (unitDef != null) ? Mathf.Max(1, unitDef.baseCapacity) : 1;
        if (battleRunning && reserveReady && attackersActive < cap)
            ReleaseReserveToBattle();
    }

    public void StopBattle()
    {
        battleRunning = false;
        StopAllCoroutines();
        SetSpawnerAlpha(1f);

        // Reset UI
        SetProgressImmediate(0f);
    }

    public void ResetForNewRound()
    {
        StopAllCoroutines();
        battleRunning = false;
        attackersActive = 0;
        reserveReady = false;
        isSpawningReserve = false;

        if (reserveStats != null)
        {
            Destroy(reserveStats.gameObject);
            reserveStats = null;
        }

        SetSpawnerAlpha(1f);

        // NEW: Ensure color and progress are reset for next round [9]
        ResetProgressColor();
        SetProgressImmediate(0f);
    }

    private void SetSpawnerAlpha(float alpha)
    {
        alpha = Mathf.Clamp01(alpha);
        var renderers = GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var r in renderers)
        {
            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }
    }

    // -------------------------
    // Progress UI helpers
    // -------------------------
    public void AttachCellProgressImage(DropAreaCell cell)
    {
        if (cell == null) return;
        spawnProgressFill = cell.fillImage;

        // NEW: cache original color now that we actually have the image
        if (spawnProgressFill != null)
            defaultProgressColor = spawnProgressFill.color;
    }

    private void SetProgressTarget(float value01)
    {
        progressTarget = Mathf.Clamp01(value01);

        if (spawnProgressFill == null)
            return;

        if (progressRoutine == null)
            progressRoutine = StartCoroutine(ProgressSmoothRoutine());
    }

    private void SetProgressImmediate(float value01)
    {
        progressTarget = Mathf.Clamp01(value01);

        if (spawnProgressFill != null)
            spawnProgressFill.fillAmount = progressTarget;

        if (progressRoutine != null)
        {
            StopCoroutine(progressRoutine);
            progressRoutine = null;
        }
    }

    private IEnumerator ProgressSmoothRoutine()
    {
        while (spawnProgressFill != null && battleRunning)
        {
            float current = spawnProgressFill.fillAmount;
            float next = Mathf.Lerp(current, progressTarget, Time.deltaTime * progressLerpSpeed);
            spawnProgressFill.fillAmount = next;

            if (Mathf.Abs(next - progressTarget) < 0.002f)
            {
                spawnProgressFill.fillAmount = progressTarget;

                if (progressTarget <= 0.0001f || progressTarget >= 0.9999f)
                    break;
            }

            yield return null;
        }

        progressRoutine = null;
    }

    private void ApplyUpgradesToSpawnedUnit(GameObject go, CharacterStats stats)
    {
        if (go == null || stats == null) return;

        var state = GetComponent<UnitSpawnerBattleUpgradeState>();
        if (state == null || state.pickedNodes == null || state.pickedNodes.Count == 0)
            return;

        // 1) Apply gameplay effects (placeholder - implement per nodeId)
        for (int i = 0; i < state.pickedNodes.Count; i++)
        {
            var node = state.pickedNodes[i];
            if (node == null) continue;

            // Example hooks:
            // if (node.nodeId == "atkspd_lowhp") { ... }
            // if (node.nodeId == "lifesteal_5") { ... }
        }

        // 2) Sync UI icons on the spawned unit (recommended)
        var upgradesUI = go.GetComponentInChildren<BattleUpgradesManager>(true);
        if (upgradesUI != null)
        {
            upgradesUI.Clear();
            for (int i = 0; i < state.pickedNodes.Count; i++)
                upgradesUI.AddUpgradeIcon(state.pickedNodes[i]);
        }

        foreach (var n in state.pickedNodes)
        {
            if (n == null || n.skillEffect == null) continue;

            var type = n.skillEffect.GetType();

            if (go.GetComponent(type) != null) continue;

            var comp = (UnitSkillBehaviour)go.AddComponent(type);
            comp.Init(stats, n);
        }



    }
    public void SellSpawner()
    {
        if (battleRunning)
            return;

        if (unitDef == null)
            return;

        int totalCost = GetTotalSoulCost();
        int refund = Mathf.FloorToInt(totalCost * 0.5f);

        if (refund > 0 && SoulsManager.instance != null)
            SoulsManager.instance.AddSouls(refund);

        BattleCellSelectionController.Instance.ClearSelection();
        BattleBottomPanelController.Instance.HideSellButton();
        BattleBottomPanelController.Instance.ShowPlaceholderText(BattleBottomPanelController.Instance.defaultPlaceholder);

        Destroy(gameObject);
        BattleManager.instance.StartCoroutine(RefreshNextFrame());

    }
    private IEnumerator RefreshNextFrame()
    {
        yield return null;
        BattleManager.instance.RefreshStartBattleButton();
    }

    private void SetReserveCollision(GameObject go, bool enabled)
    {
        if (go == null) return;

        // disable/enable ALL colliders on the unit (including children)
        var cols = go.GetComponentsInChildren<Collider2D>(true);
        for (int i = 0; i < cols.Length; i++)
            cols[i].enabled = enabled;

        // optional but recommended: also stop physics while waiting
        var rb = go.GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.simulated = enabled;
    }
    public bool TryBuyUpgrade(UnitUpgradeNodeDefinition node)
    {
        if (node == null) return false;

        if (battleRunning)
            return false;

        int cost = Mathf.Max(0, node.soulsCost);

        if (cost > 0)
        {
            if (SoulsManager.instance == null)
                return false;

            if (!SoulsManager.instance.TrySpend(cost))
            {
                ToastManager.Instance.Show("Not enough souls!");
                Debug.Log("Not enough souls");
                return false;
            }
        }

        var state = GetComponent<UnitSpawnerBattleUpgradeState>();
        if (state == null)
            state = gameObject.AddComponent<UnitSpawnerBattleUpgradeState>();

        state.Pick(node);

        return true;
    }
    private int GetTotalSoulCost()
    {
        int total = 0;

        if (unitDef != null)
            total += Mathf.Max(0, unitDef.soulCost);

        var state = GetComponent<UnitSpawnerBattleUpgradeState>();
        if (state != null && state.pickedNodes != null)
        {
            for (int i = 0; i < state.pickedNodes.Count; i++)
            {
                var node = state.pickedNodes[i];
                if (node == null) continue;
                total += Mathf.Max(0, node.soulsCost);
            }
        }

        return total;
    }


    public void SetManualProgress(float value01)
    {
        // Allows external units to drive the progress bar (0 to 1) [5]
        SetProgressTarget(value01);
        if (spawnProgressFill != null)
        {
            spawnProgressFill.fillAmount = value01;
        }
    }

    public void SetProgressColor(Color newColor)
    {
        // Changes the bar color (e.g., to Blue for Miner) [4]
        if (spawnProgressFill != null)
        {
            spawnProgressFill.color = newColor;
        }
    }

    public void ResetProgressColor()
    {
        // Reverts the bar to its original unit-creation color [6]
        if (spawnProgressFill != null)
        {
            spawnProgressFill.color = defaultProgressColor;
        }
    }

    private void InitExistingSkillBehaviours(GameObject go, CharacterStats stats)
    {
        if (go == null || stats == null) return;

        var skills = go.GetComponents<UnitSkillBehaviour>();
        for (int i = 0; i < skills.Length; i++)
        {
            if (skills[i] == null) continue;

            // Node is null because this is a built-in behaviour on the prefab (not an upgrade node)
            skills[i].Init(stats, null);
        }
    }

}
