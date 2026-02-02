using UnityEngine;

public class HealProjectile : MonoBehaviour
{
    public float speed = 8f;
    public float arcHeight = 2f;

    private CharacterStats healer;
    private CharacterStats target;
    private int healAmount;

    private Vector3 flatPos;
    private float time;
    private float travelDuration;

    public void Init(CharacterStats healerStats, CharacterStats targetStats, int healAmount)
    {
        healer = healerStats;
        target = targetStats;
        this.healAmount = Mathf.Max(0, healAmount);

        Vector3 startPos = transform.position;
        flatPos = startPos;
        time = 0f;

        float distance = Vector3.Distance(startPos, target.transform.position);
        travelDuration = distance / Mathf.Max(0.01f, speed);
        if (travelDuration <= 0f) travelDuration = 0.1f;
    }

    private void Update()
    {
        if (target == null || !target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        time += Time.deltaTime;
        float t = Mathf.Clamp01(time / travelDuration);

        Vector3 targetPos = target.transform.position;
        Vector3 dir = (targetPos - flatPos).normalized;
        flatPos += dir * speed * Time.deltaTime;

        float height = 4f * arcHeight * t * (1f - t);
        Vector3 nextPos = flatPos + Vector3.up * height;

        transform.position = nextPos;

        if (Vector3.Distance(transform.position, targetPos) < 0.2f || t >= 1f)
            HitTarget();
    }

    private void HitTarget()
    {
        if (target != null && target.IsAlive)
            target.Heal(healAmount, healer);

        Destroy(gameObject);
    }
}
