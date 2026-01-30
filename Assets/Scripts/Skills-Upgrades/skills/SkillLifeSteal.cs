using UnityEngine;

public class SkillLifeSteal : UnitSkillBehaviour
{
    [Range(0f, 1f)]
    public float lifeStealPercent = 0.2f; // 20%

    protected override void OnInit()
    {
        stats.OnDealDamage += HandleDealDamage;
    }

    private void HandleDealDamage(CharacterStats target, int damage)
    {
        if (stats == null) return;

        int heal = Mathf.RoundToInt(damage * lifeStealPercent);
        if (heal <= 0) return;

        stats.currentHealth = Mathf.Min(stats.currentHealth + heal, stats.maxHealth);
        Debug.Log($"Life stolen {heal}");
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnDealDamage -= HandleDealDamage;
    }
}
