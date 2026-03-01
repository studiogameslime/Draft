using UnityEngine;

public class StatusEffectPoison : MonoBehaviour
{
    private CharacterStats target;
    private CharacterStats source;

    private float duration;
    private float timer;

    private float tickInterval = 1f;
    private float tickTimer;

    private int damagePerTick;

    [SerializeField] private Color poisonColor = new Color(0.87f, 0.64f, 0.96f);

    public void Apply(
        CharacterStats attacker,
        CharacterStats targetStats,
        float percentPerSecond,
        float durationSeconds
    )
    {
        source = attacker;
        target = targetStats;

        // Refresh total effect duration
        duration = Mathf.Max(0.1f, durationSeconds);
        timer = duration;

        // Recalculate damage based on attacker's current stats
        damagePerTick = Mathf.RoundToInt(
            Mathf.Max(0f, attacker.damage) * Mathf.Clamp01(percentPerSecond)
        );

        // Apply visual feedback
        target.SetUnitColor(poisonColor);

        /* 
         * REMOVED: Destroy(this, durationSeconds);
         * Logic: Using Destroy with a delay schedules a fixed destruction time. 
         * We now let the Update loop handle destruction via the 'timer' variable 
         * to allow refreshing the duration on multiple hits.
         */
    }

    private void Update()
    {
        // Safety check for target validity
        if (target == null || !target.IsAlive)
        {
            Destroy(this);
            return;
        }

        // Handle total duration countdown
        timer -= Time.deltaTime;

        // Handle periodic damage intervals
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            DoTick();
        }

        // Destroy effect when duration expires
        if (timer <= 0f)
        {
            Destroy(this);
        }
    }

    private void DoTick()
    {
        if (damagePerTick <= 0) return;

        // Apply damage through the core stats system with poison floating text
        target.ApplyStatusDamage(damagePerTick, source, FloatingNumberType.Poison);
        Debug.Log($"Poison tick: {damagePerTick} damage dealt to {target.name}");
    }

    private void OnDestroy()
    {
        // Revert unit color to default when effect ends
        if (target != null)
        {
            target.ResetColor();
        }
    }
}