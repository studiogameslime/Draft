using UnityEngine;

public class SkillTaunt : UnitSkillBehaviour
{
    private float tauntRadius;
    private float tauntDuration;
    private float tauntCooldown;
    private float timer;

    protected override void OnInit()
    {
        if (stats == null || node == null) return;

        // Map values from the upgrade node definition
        tauntRadius = Mathf.Max(1f, node.skillRadiusValue);
        tauntDuration = Mathf.Max(0.5f, node.skillDuration);
        tauntCooldown = Mathf.Max(1f, node.skillIntValue);

        // Start the timer at half cooldown so the knight taunts shortly after spawning
        timer = tauntCooldown * 0.5f;
    }

    private void Update()
    {
        if (stats == null || stats.currentHealth <= 0) return;
        if (BattleManager.instance == null || !BattleManager.instance.IsBattleRunning) return;

        // ADDED: Prevent the unit from casting taunt while waiting in the spawner
        if (stats.isUntargetable) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = tauntCooldown;
            CastTaunt();
        }
    }

    private void CastTaunt()
    {
        // Find all units currently on the board
        var allUnits = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        foreach (var unit in allUnits)
        {
            if (unit == null || unit == stats || !unit.IsAlive) continue;

            // Only affect enemies
            if (unit.team == stats.team) continue;

            // Ignore untargetable units (like the gold miner)
            if (unit.isUntargetable) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist <= tauntRadius)
            {
                var ai = unit.GetComponent<UnitAI>();
                if (ai != null)
                {
                    // Apply the taunt effect to this specific enemy
                    ai.ApplyTaunt(stats, tauntDuration);
                }
            }
        }
    }
}