using UnityEngine;

public class SkillShieldBash : UnitSkillBehaviour
{
    private float knockbackChance;
    private float knockbackDistance;
    private float knockbackDuration;

    protected override void OnInit()
    {
        if (stats == null || node == null) return;

        // Map values from the upgrade node definition
        knockbackChance = Mathf.Clamp01(node.skillPercentValue);
        knockbackDistance = Mathf.Max(0.1f, node.skillRadiusValue);
        knockbackDuration = Mathf.Max(0.05f, node.skillDuration);

        // Subscribe to the deal damage event
        stats.OnDealDamage += HandleDealDamage;
    }

    private void HandleDealDamage(CharacterStats target, int damage)
    {
        if (target == null || !target.IsAlive) return;

        // Roll the chance to apply knockback
        if (Random.value > knockbackChance) return;

        // Calculate direction away from the Knight
        Vector2 dir = (target.transform.position - transform.position).normalized;

        // Apply knockback based on target type
        var unitAI = target.GetComponent<UnitAI>();
        if (unitAI != null)
        {
            unitAI.ApplyKnockback(dir, knockbackDistance, knockbackDuration);
        }
        else
        {
            var fairyAI = target.GetComponent<FairyAI>();
            if (fairyAI != null)
            {
                fairyAI.ApplyKnockback(dir, knockbackDistance, knockbackDuration);
            }
        }
    }

    private void OnDestroy()
    {
        // Cleanup event subscription
        if (stats != null)
        {
            stats.OnDealDamage -= HandleDealDamage;
        }
    }
}