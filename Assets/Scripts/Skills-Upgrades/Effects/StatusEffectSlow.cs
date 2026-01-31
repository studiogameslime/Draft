using UnityEngine;

public class StatusEffectSlow : MonoBehaviour
{
    private CharacterStats target;

    private float slowPercent;   // 0.8 = 80%
    private float duration;

    private float baseMoveSpeed;
    private float timer;

    private bool initialized = false;

    public void Apply(CharacterStats targetStats, float slowPercent, float duration)
    {
        target = targetStats;
        this.slowPercent = Mathf.Clamp01(slowPercent);
        this.duration = duration;

        if (!initialized)
        {
            initialized = true;

            baseMoveSpeed = target.moveSpeed;

            target.moveSpeed = baseMoveSpeed * (1f - this.slowPercent);
        }

        timer = this.duration;
    }

    private void Update()
    {
        if (target == null)
        {
            Destroy(this);
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
        if (target != null)
        {
            target.moveSpeed = baseMoveSpeed;
        }

        Destroy(this);
    }

    private void OnDestroy()
    {
        if (target != null)
        {
            target.moveSpeed = baseMoveSpeed;
        }
    }
}
