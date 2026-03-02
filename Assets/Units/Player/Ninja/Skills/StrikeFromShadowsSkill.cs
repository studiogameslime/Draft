using UnityEngine;

/// Tier 1 Skill: After completing a stealth run, the Ninja's first attack deals 3x damage.
public class StrikeFromShadowsSkill : UnitSkillBehaviour
{
    private NinjaStealthRun stealthRun;
    private bool nextAttackEmpowered;
    private float damageMultiplier = 2f;

    protected override void OnInit()
    {
        stealthRun = GetComponent<NinjaStealthRun>();
        if (stealthRun == null) return;

        stealthRun.OnStealthRunComplete += HandleStealthComplete;
        stats.OnDealDamage += HandleDealDamage;
    }

    private void HandleStealthComplete()
    {
        nextAttackEmpowered = true;
    }

    private void HandleDealDamage(CharacterStats target, int damage)
    {
        if (!nextAttackEmpowered) return;
        if (target == null || !target.IsAlive) return;

        nextAttackEmpowered = false;

        // Apply bonus damage (original hit already dealt 1x, add the remaining 2x)
        int bonusDamage = Mathf.RoundToInt(damage * (damageMultiplier - 1f));
        if (bonusDamage > 0)
            target.TakeDamage(bonusDamage, stats);
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnDealDamage -= HandleDealDamage;

        if (stealthRun != null)
            stealthRun.OnStealthRunComplete -= HandleStealthComplete;
    }
}
