using UnityEngine;

public class SkillBonusDamagePercent : UnitSkillBehaviour
{
    protected override void OnInit()
    {
        if (stats == null || node == null) return;

        // Map values from the upgrade node definition (e.g., 0.2 for 20%)
        float percentMultiplier = Mathf.Clamp(node.skillPercentValue, 0f, 10f);

        // Calculate the extra damage
        // Note: For the Fairy unit, 'damage' acts as her healing power
        int extraDamage = Mathf.RoundToInt(stats.damage * percentMultiplier);

        // Apply the bonus directly to the unit's stats
        stats.damage += extraDamage;
    }
}