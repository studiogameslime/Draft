using UnityEngine;

public class MeteorProjectile : MonoBehaviour
{
    [Header("Movement")]
    public float fallSpeed = 10f;       // how fast the meteor falls
    public float impactRadius = 0.2f;   // distance to target before we consider it a hit

    [Header("VFX")]
    public GameObject impactVfxPrefab;  // optional impact effect

    private CharacterStats target;
    private int damage;

    /// <summary>
    /// Initializes this meteor with a target and damage value.
    /// </summary>
    public void Init(CharacterStats targetStats, int damageAmount)
    {
        target = targetStats;
        damage = damageAmount;
        transform.Rotate(new Vector3(0, 0, 90));
    }

    private void Update()
    {
        if (target == null || target.currentHealth <= 0)
        {
            // Target died or disappeared, just destroy the meteor
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
        // Deal damage
        if (target != null && target.currentHealth > 0)
        {
            target.TakeDamage(damage);
        }

        // Spawn impact VFX if assigned
        if (impactVfxPrefab != null)
        {
            Instantiate(impactVfxPrefab, hitPosition, Quaternion.identity);
        }

        // Destroy the meteor object
        Destroy(gameObject);
    }
}
