using UnityEngine;

public class SkillPoisonArrow : UnitSkillBehaviour, IOnHitEffect, IOnProjectileSpawned
{
    private float percentPerSecond;
    private float duration;

    protected override void OnInit()
    {
        percentPerSecond = Mathf.Clamp01(node.skillPercentValue); // 0.15
        duration = node.skillDuration;                            // 3
    }

    public void OnHit(CharacterStats attacker, CharacterStats target)
    {
        if (target == null) return;

        var poison = target.GetComponent<StatusEffectPoison>();
        if (poison == null)
            poison = target.gameObject.AddComponent<StatusEffectPoison>();

        poison.Apply(attacker, target, percentPerSecond, duration);
    }

    public void OnProjectileSpawned(Projectile projectile)
    {
        var fx = projectile.GetComponentInChildren<ArrowEffect>(true);
        if (fx != null)
            fx.Apply(ArrowElement.Poison);
    }
}
