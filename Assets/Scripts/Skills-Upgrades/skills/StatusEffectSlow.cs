using System.Collections;
using UnityEngine;

public class StatusEffectSlow : MonoBehaviour
{
    private CharacterStats stats;
    private float slowMultiplier;
    private float duration;
    private Coroutine routine;

    public void Apply(CharacterStats target, float slowPercent, float duration)
    {
        if (target == null) return;

        stats = target;
        this.slowMultiplier = Mathf.Clamp01(1f - slowPercent); // 0.2 if 80% slow
        this.duration = duration;

        if (routine != null)
            StopCoroutine(routine);

        routine = StartCoroutine(SlowRoutine());
    }

    private IEnumerator SlowRoutine()
    {
        float originalSpeed = stats.moveSpeed;
        stats.moveSpeed *= slowMultiplier;

        yield return new WaitForSeconds(duration);

        stats.moveSpeed = originalSpeed;
        Destroy(this);
    }
}
