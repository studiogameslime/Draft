using UnityEngine;

public class SkillEnergize : UnitSkillBehaviour
{
    private float buffPercent;
    private float duration;

    protected override void OnInit()
    {
        if (stats == null || node == null) return;

        // Map values from the upgrade node definition
        buffPercent = Mathf.Clamp01(node.skillPercentValue);
        duration = Mathf.Max(0.5f, node.skillDuration);

        // Subscribe to the healing event
        stats.OnHealAlly += HandleHealAlly;
    }

    private void HandleHealAlly(CharacterStats healedTarget, int amountHealed)
    {
        if (healedTarget == null || !healedTarget.IsAlive) return;

        // Prevent fairies from energizing themselves or other units ignoring buffs (if needed)
        if (healedTarget == stats) return;

        // Add or update the energize status effect on the target
        var energize = healedTarget.GetComponent<StatusEffectEnergize>();
        if (energize == null)
        {
            energize = healedTarget.gameObject.AddComponent<StatusEffectEnergize>();
        }

        energize.Apply(healedTarget, buffPercent, duration);
    }

    private void OnDestroy()
    {
        // Cleanup event subscription
        if (stats != null)
        {
            stats.OnHealAlly -= HandleHealAlly;
        }
    }
}