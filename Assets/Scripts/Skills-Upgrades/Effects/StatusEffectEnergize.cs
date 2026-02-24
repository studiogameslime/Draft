using UnityEngine;

public class StatusEffectEnergize : MonoBehaviour
{
    private CharacterStats target;
    private float speedBuffPercent;
    private float duration;

    private float baseCooldown;
    private float timer;
    private bool initialized = false;

    public void Apply(CharacterStats targetStats, float buffPercent, float buffDuration)
    {
        target = targetStats;
        speedBuffPercent = Mathf.Clamp01(buffPercent);
        duration = buffDuration;

        if (!initialized)
        {
            initialized = true;
            // Save the original cooldown so we can restore it later
            baseCooldown = target.attackCooldown;

            // Reduce the attack cooldown (meaning faster attacks)
            target.attackCooldown = baseCooldown * (1f - speedBuffPercent);
        }

        // Reset the timer if the fairy heals this unit again before the buff expires
        timer = duration;
    }

    private void Update()
    {
        if (target == null || !target.IsAlive)
        {
            Remove();
            return;
        }

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Remove();
        }
    }

    private void Remove()
    {
        if (target != null && initialized)
        {
            // Restore original attack speed
            target.attackCooldown = baseCooldown;
        }
        Destroy(this);
    }

    private void OnDestroy()
    {
        // Safety catch in case the object is destroyed abruptly
        if (target != null && initialized)
        {
            target.attackCooldown = baseCooldown;
        }
    }
}