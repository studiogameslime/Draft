using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float fallSpeed = 10f;        // how fast the meteor falls
    public float impactRadius = 0.2f;    // distance to target before we consider it a hit

    [Header("Splash Damage")]
    public float splashRadius = 1.5f;           // radius for AoE damage
    [Range(0f, 1f)]
    public float splashDamageFraction = 0.5f;   // 0.5 = 50% of main damage
    public LayerMask splashLayerMask;           // which layers contain damageable units

    [Header("VFX")]
    public GameObject impactVfxPrefab;          // optional impact effect

    private CharacterStats target;
    private int damage;
    private Team attackerTeam;


    /// <summary>
    /// Initializes this meteor with a target, damage value and the attacker's team.
    /// </summary>
    public void Init(CharacterStats targetStats, int damageAmount, Team attackerTeamValue)
    {
        target = targetStats;
        damage = damageAmount;
        attackerTeam = attackerTeamValue;

        // Optional: rotate visual
        transform.Rotate(new Vector3(0, 0, 90));
    }

    private void Update()
    {
        if (target == null || target.currentHealth <= 0)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 targetPos = target.transform.position;

        // Move straight towards the target position
        transform.position = Vector3.MoveTowards(
            transform.position,
            targetPos,
            fallSpeed * Time.deltaTime
        );

        // Check if we are close enough to count as an impact
        if (Vector3.Distance(transform.position, targetPos) <= impactRadius)
        {
            OnImpact(targetPos);
        }
    }

    private void OnImpact(Vector3 hitPosition)
    {
        // Direct hit on main target
        if (target != null && target.currentHealth > 0)
        {
            target.TakeDamage(damage);
        }

        // Compute ground position under the target (explosion center)
        Vector3 groundPos = hitPosition;

        if (target != null)
        {
            Collider2D col = target.GetComponent<Collider2D>();
            if (col != null)
            {
                Vector3 bottom = col.bounds.min;
                groundPos = new Vector3(bottom.x, bottom.y, hitPosition.z);
            }
        }

        // Splash damage around ground position
        ApplySplashDamage(groundPos);

        // Spawn impact VFX if assigned, on the ground
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, groundPos, Quaternion.identity);
        }

        // Destroy the meteor object
        Destroy(gameObject);
    }

    private void ApplySplashDamage(Vector3 center)
    {
        if (splashRadius <= 0f || splashDamageFraction <= 0f)
            return;

        int splashDamage = Mathf.RoundToInt(damage * splashDamageFraction);
        if (splashDamage <= 0)
            return;

        // Physics2D AoE search
        Collider2D[] hits = Physics2D.OverlapCircleAll(center, splashRadius, splashLayerMask);
        if (hits == null || hits.Length == 0)
            return;

        foreach (var hit in hits)
        {
            CharacterStats otherStats = hit.GetComponent<CharacterStats>();
            if (otherStats == null)
                continue;

            // Skip dead units
            if (otherStats.currentHealth <= 0)
                continue;

            // Only damage enemies (not allies)
            if (otherStats.team == attackerTeam)
                continue;

            // Full damage was already applied to the main target
            if (otherStats == target)
                continue;

            otherStats.TakeDamage(splashDamage);
        }
    }


#if UNITY_EDITOR
    private void OnDrawGizmosSelected()
    {
        // Impact radius
        Gizmos.color = Color.yellow;
        Gizmos.DrawWireSphere(transform.position, impactRadius);

        // Splash radius
        Gizmos.color = new Color(1f, 0.5f, 0f, 0.4f);
        Gizmos.DrawWireSphere(transform.position, splashRadius);
    }
#endif
}
