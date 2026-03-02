using UnityEngine;

public class SkillLifeSteal : UnitSkillBehaviour
{
    private float lifeStealPercent;

    protected override void OnInit()
    {
        lifeStealPercent = Mathf.Clamp(node.skillPercentValue, 0f, 1f);

        stats.OnDealDamage += HandleDealDamage;
    }

    private void HandleDealDamage(CharacterStats target, int damage)
    {
        if (stats == null) return;

        int heal = Mathf.RoundToInt(damage * lifeStealPercent);
        if (heal <= 0) return;

        stats.currentHealth = Mathf.Min(
            stats.currentHealth + heal,
            stats.maxHealth
        );
        stats.showFloatingDamage(heal, FloatingNumberType.Heal);
        Debug.Log($"Health gain = {heal}");
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnDealDamage -= HandleDealDamage;
    }
}
