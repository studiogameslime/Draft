using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float fallSpeed = 10f;
    public float impactRadius = 0.2f;

    [Header("Splash Damage")]
    public float splashRadius = 1.5f;
    [Range(0f, 1f)] public float splashDamageFraction = 0f;
    public LayerMask splashLayerMask;

    [Header("VFX")]
    public GameObject impactVfxPrefab;

    private ICombatTarget target;
    private int damage;
    private CharacterStats attacker;
    private Team attackerTeam;

    public ICombatTarget Target => target;

    /// <summary>
    /// Initializes this meteor with a target, damage and attacker (for correct damage attribution).
    /// </summary>
    public void Init(ICombatTarget targetTarget, int damageAmount, CharacterStats attackerStats)
    {
        target = targetTarget;
        damage = damageAmount;
        attacker = attackerStats;
        attackerTeam = attackerStats != null ? attackerStats.team : Team.MyTeam;

        transform.Rotate(new Vector3(0, 0, 90));
    }

    private void Update()
    {
        if (target == null || !target.IsAlive || target.TargetTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.TargetTransform.position;

        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            fallSpeed * Time.deltaTime
        );

        if (Vector3.Distance(transform.position, targetPos) <= impactRadius)
        {
            OnImpact(targetPos);
        }
    }

    private void OnImpact(Vector3 hitPosition)
    {
        if (target != null && target.IsAlive)
        {
            target.TakeDamage(damage, attacker);

            if (attacker != null && target is CharacterStats hitStats)
            {
                var effects = attacker.GetComponents<IOnHitEffect>();
                foreach (var e in effects) e.OnHit(attacker, hitStats);
            }
        }

        Vector3 center = ComputeImpactCenter(hitPosition);

        ApplySplashDamage(center);

        if (impactVfxPrefab != null)
            Instantiate(impactVfxPrefab, center, Quaternion.identity);

        Destroy(gameObject);
    }

    private Vector3 ComputeImpactCenter(Vector3 fallback)
    {
        if (target == null || target.TargetTransform == null)
            return fallback;

        if (target is CharacterStats cs)
        {
            var col = cs.GetComponent<Collider2D>();
            if (col != null)
            {
                Vector3 bottom = col.bounds.min;
                return new Vector3(cs.transform.position.x, bottom.y, fallback.z);
            }
            return cs.transform.position;
        }

        return target.TargetTransform.position;
    }

    private void ApplySplashDamage(Vector3 center)
    {
        if (splashRadius <= 0f || splashDamageFraction <= 0f) return;

        int splashDamage = Mathf.RoundToInt(damage * splashDamageFraction);
        if (splashDamage <= 0) return;

        Collider2D[] hits = Physics2D.OverlapCircleAll(center, splashRadius, splashLayerMask);
        if (hits == null || hits.Length == 0) return;

        foreach (var hit in hits)
        {
            CharacterStats other = hit.GetComponent<CharacterStats>();
            if (other == null) continue;
            if (other.currentHealth <= 0) continue;
            if (other.isUntargetable) continue;

            if (other.team == attackerTeam) continue;

            if (target is CharacterStats mainUnit && other == mainUnit) continue;

            other.TakeDamage(splashDamage, attacker);
        }
    }

#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, impactRadius);

        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
#endif
}
