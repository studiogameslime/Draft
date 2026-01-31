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

    public void Apply(
        CharacterStats attacker,
        CharacterStats targetStats,
        float percentPerSecond,
        float durationSeconds
    )
    {
        source = attacker;
        target = targetStats;

        duration = Mathf.Max(0.1f, durationSeconds);
        timer = duration;

        damagePerTick = Mathf.RoundToInt(
            Mathf.Max(0f, attacker.damage) * Mathf.Clamp01(percentPerSecond)
        );

        tickTimer = 0f;
    }

    private void Update()
    {
        if (target == null || !target.IsAlive)
        {
            Destroy(this);
            return;
        }

        timer -= Time.deltaTime;
        tickTimer += Time.deltaTime;

        if (tickTimer >= tickInterval)
        {
            tickTimer -= tickInterval;
            DoTick();
        }

        if (timer <= 0f)
        {
            Destroy(this);
        }
    }

    private void DoTick()
    {
        if (damagePerTick <= 0) return;

        target.ApplyStatusDamage(damagePerTick, source);
        Debug.Log($"Poison tick: {damagePerTick}");
    }
}
