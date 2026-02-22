using System.Collections;
using UnityEngine;

public class MinerBehaviour : UnitSkillBehaviour
{
    [SerializeField] private Color soulProductionColor = Color.blue;

    private UnitSpawner mySpawner;
    private float productionTimer;

    protected override void OnInit()
    {
        // Disable AI as the Miner is a static structure
        var ai = GetComponent<UnitAI>();
        if (ai != null) ai.enabled = false;

        TryResolveSpawner();

        // If spawner isn't ready yet (link.owner assigned after Instantiate), wait one frame
        if (mySpawner == null)
            StartCoroutine(ResolveSpawnerNextFrame());
        else
            ApplySpawnerVisuals();
    }

    private IEnumerator ResolveSpawnerNextFrame()
    {
        yield return null;
        TryResolveSpawner();
        ApplySpawnerVisuals();
    }

    private void TryResolveSpawner()
    {
        var link = GetComponent<SpawnedUnitOwnerLink>();
        if (link != null)
            mySpawner = link.owner;
    }

    private void ApplySpawnerVisuals()
    {
        if (mySpawner == null) return;
        mySpawner.SetProgressColor(soulProductionColor);
        mySpawner.SetManualProgress(0f);
    }

    private void Update()
    {
        if (stats == null) return;
        if (BattleManager.instance == null) return;
        if (!BattleManager.instance.IsBattleRunning) return;
        if (!stats.IsAlive) return;

        float interval = Mathf.Max(0.05f, stats.attackCooldown);
        productionTimer += Time.deltaTime;

        float progress = Mathf.Clamp01(productionTimer / interval);
        if (mySpawner != null)
            mySpawner.SetManualProgress(progress);

        if (productionTimer >= interval)
        {
            productionTimer = 0f;

            if (SoulsManager.instance != null)
                SoulsManager.instance.AddSouls(1);

            stats.showFloatingDamage(1, FloatingNumberType.Heal);
        }
    }

    private void OnDestroy()
    {
        if (mySpawner != null)
        {
            mySpawner.ResetProgressColor();
            mySpawner.SetManualProgress(0f);
        }
    }
}