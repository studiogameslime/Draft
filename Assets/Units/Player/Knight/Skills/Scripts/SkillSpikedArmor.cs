using UnityEngine;

public class SkillSpikedArmor : UnitSkillBehaviour
{
    private float returnDamagePercent;
    private float meleeRangeThreshold;

    protected override void OnInit()
    {
        if (stats == null || node == null) return;

        // Map values from the upgrade node definition
        returnDamagePercent = Mathf.Clamp01(node.skillPercentValue);
        meleeRangeThreshold = Mathf.Max(0.5f, node.skillRadiusValue);

        // Subscribe to the take damage event
        stats.OnTakeDamage += HandleTakeDamage;
    }

    private void HandleTakeDamage(CharacterStats attacker, int damageTaken)
    {
        if (attacker == null || attacker == stats || !attacker.IsAlive) return;

        // Check distance to ensure it is a melee attack
        float dist = Vector3.Distance(transform.position, attacker.transform.position);
        if (dist > meleeRangeThreshold) return;

        // Calculate damage to return
        int returnDamage = Mathf.RoundToInt(damageTaken * returnDamagePercent);

        if (returnDamage > 0)
        {
            // Deal damage back to the attacker
            attacker.TakeDamage(returnDamage, stats);
        }
    }

    private void OnDestroy()
    {
        // Cleanup event subscription
        if (stats != null)
        {
            stats.OnTakeDamage -= HandleTakeDamage;
        }
    }
}