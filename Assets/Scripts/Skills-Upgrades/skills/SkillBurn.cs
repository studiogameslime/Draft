using UnityEngine;

public class SkillBurn : UnitSkillBehaviour, IOnHitEffect
{
    private float percentPerSecond;
    private float duration;

    protected override void OnInit()
    {
        percentPerSecond = Mathf.Clamp01(node.skillPercentValue);
        duration = node.skillDuration;
    }

    public void OnHit(CharacterStats attacker, CharacterStats target)
    {
        if (target == null) return;

        var burn = target.GetComponent<StatusEffectBurn>();
        if (burn == null)
            burn = target.gameObject.AddComponent<StatusEffectBurn>();

        burn.Apply(attacker, target, percentPerSecond, duration);
    }
}
