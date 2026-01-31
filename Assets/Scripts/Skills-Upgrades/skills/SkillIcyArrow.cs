using UnityEngine;

public class SkillIcyArrow : UnitSkillBehaviour, IOnHitEffect
{
    private float slowPercent;
    private float slowDuration;

    protected override void OnInit()
    {
        slowPercent = Mathf.Clamp01(node.skillPercentValue); // 0.8
        slowDuration = node.skillDuration;                   // 1.5
    }

    public void OnHit(CharacterStats attacker, CharacterStats target)
    {
        if (target == null) return;

        var slow = target.GetComponent<StatusEffectSlow>();
        if (slow == null)
        {
            slow = target.gameObject.AddComponent<StatusEffectSlow>();
        }

        slow.Apply(target, slowPercent, slowDuration);
    }
}
