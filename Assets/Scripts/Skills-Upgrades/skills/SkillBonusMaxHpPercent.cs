using UnityEngine;

public class SkillBonusMaxHpPercent : UnitSkillBehaviour
{
    protected override void OnInit()
    {
        float p = Mathf.Clamp(node.skillPercentValue, 0f, 10f); // value01 = 0.1 => 10%
        int newMax = Mathf.RoundToInt(stats.maxHealth * (1f + p));
        int delta = newMax - stats.maxHealth;

        stats.maxHealth = newMax;
        stats.currentHealth += delta;
    }
}
