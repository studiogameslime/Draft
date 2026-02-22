using UnityEngine;

// This behavior controls the Gold Miner. 
// It handles soul production and updates the cell's progress bar in blue.
public class MinerBehaviour : UnitSkillBehaviour
{
    private float productionTimer;
    private UnitSpawner mySpawner;
    private Color soulProductionColor = Color.blue;

    protected override void OnInit()
    {
        // Disable AI as the Miner is a static structure
        var ai = GetComponent<UnitAI>();
        if (ai != null) ai.enabled = false;

        // Find the spawner that owns this unit via the owner link
        var link = GetComponent<SpawnedUnitOwnerLink>();
        if (link != null)
        {
            mySpawner = link.owner;
            
            // Set the progress bar color to Blue as requested
            if (mySpawner != null)
            {
                mySpawner.SetProgressColor(soulProductionColor);
            }
        }
    }

    private void Update()
    {
        if (stats == null) return;
        // Only produce souls if the battle is running and unit is alive
        if (!BattleManager.instance.IsBattleRunning || !stats.IsAlive) return;

        productionTimer += Time.deltaTime;

        // Use attackCooldown from definition as the soul production interval
        float interval = stats.attackCooldown;
        float progress = Mathf.Clamp01(productionTimer / interval);

        // Update the visual UI on the grid cell manually
        if (mySpawner != null)
        {
            mySpawner.SetManualProgress(progress);
        }

        if (productionTimer >= interval)
        {
            productionTimer = 0f;
            ProduceSoul();
        }
    }

    private void ProduceSoul()
    {
        if (SoulsManager.instance != null)
        {
            SoulsManager.instance.AddSouls(1);
            // Visual feedback using the existing floating text system
            stats.showFloatingDamage(1, FloatingNumberType.Heal); 
        }
    }

    private void OnDestroy()
    {
        // Clean up: Reset spawner color when the miner is removed/round ends
        if (mySpawner != null)
        {
            mySpawner.ResetProgressColor();
            mySpawner.SetManualProgress(0f);
        }
    }
}