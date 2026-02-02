using UnityEngine;

public class SkillFuryBelowHalfHp : UnitSkillBehaviour
{
    private const float Threshold01 = 0.5f;

    private float baseCooldown;
    private bool applied;

    protected override void OnInit()
    {
        if (stats == null) return;

        baseCooldown = stats.attackCooldown;
        applied = false;

        Refresh();
    }

    private void Update()
    {
        Refresh();
    }

    private void Refresh()
    {
        if (stats == null) return;

        bool shouldApply = stats.currentHealth <= stats.maxHealth * Threshold01;

        if (shouldApply && !applied)
        {
            stats.attackCooldown = baseCooldown * 0.5f;
            applied = true;
        }
        else if (!shouldApply && applied)
        {
            stats.attackCooldown = baseCooldown;
            applied = false;
        }
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.attackCooldown = baseCooldown;
    }
}
