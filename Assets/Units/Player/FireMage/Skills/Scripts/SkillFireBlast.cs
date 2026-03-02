using UnityEngine;

public class SkillFireBlast : UnitSkillBehaviour, IOnHitEffect
{
    private float damageFraction;
    private float radius;

    protected override void OnInit()
    {
        damageFraction = Mathf.Clamp01(node.skillPercentValue);
        radius = node.skillRadiusValue;
    }

    public void OnHit(CharacterStats attacker, CharacterStats target)
    {
        if (target == null || attacker == null) return;
        if (radius <= 0f || damageFraction <= 0f) return;

        int splashDmg = Mathf.RoundToInt(attacker.damage * damageFraction);
        if (splashDmg <= 0) return;

        Vector3 center = target.transform.position;
        Team enemyTeam = (attacker.team == Team.MyTeam) ? Team.EnemyTeam : Team.MyTeam;

        var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        foreach (var u in all)
        {
            if (u == null || u == target) continue;
            if (u.team != enemyTeam) continue;
            if (u.currentHealth <= 0) continue;
            if (u.isUntargetable) continue;

            float d = Vector3.Distance(center, u.transform.position);
            if (d <= radius)
                u.TakeDamage(splashDmg, attacker);
        }
    }
}
