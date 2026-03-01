using UnityEngine;

public class StatusEffectSlow : MonoBehaviour
{
    private CharacterStats target;

    private float slowPercent;   // Example: 0.8 = 80% speed reduction
    private float duration;

    private float baseMoveSpeed;
    private float timer;

    private bool initialized = false;

    [SerializeField] private Color iceColor = new Color(0.4f, 0.7f, 1f);

    public void Apply(CharacterStats targetStats, float slowPercent, float duration)
    {
        target = targetStats;
        this.slowPercent = Mathf.Clamp01(slowPercent);
        this.duration = duration;

        // Apply speed reduction only on the first hit to avoid compounding slows
        if (!initialized)
        {
            initialized = true;

            // Store original speed to ensure accurate restoration later [1, 2]
            baseMoveSpeed = target.moveSpeed;

            // Reduce target movement speed based on percentage [3]
            target.moveSpeed = baseMoveSpeed * (1f - this.slowPercent);
        }

        // Refresh the duration timer to the full duration on every hit
        timer = this.duration;

        // Apply the blue ice visual feedback [4]
        target.SetUnitColor(iceColor);

        /* 
         * REMOVED: Destroy(this, duration);
         * Logic: We no longer use delayed destruction here. 
         * The Update loop now handles destruction via the 'timer' variable,
         * allowing subsequent hits to "refresh" the effect duration.
         */
    }

    private void Update()
    {
        // Safety check for target validity or death [5]
        if (target == null || !target.IsAlive)
        {
            Destroy(this);
            return;
        }

        // Countdown the refreshed timer
        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            Remove();
        }
    }

    private void Remove()
    {
        // Explicitly restore original stats before component removal [6]
        if (target != null && initialized)
        {
            target.moveSpeed = baseMoveSpeed;
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        // Final cleanup to ensure unit returns to normal state [6]
        if (target != null)
        {
            if (initialized)
            {
                target.moveSpeed = baseMoveSpeed;
            }
            target.ResetColor();
        }
    }
}