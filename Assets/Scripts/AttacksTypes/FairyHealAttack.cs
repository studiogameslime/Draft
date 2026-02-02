using UnityEngine;

public class FairyHealAttack : MonoBehaviour, IAttackStrategy
{
    [Header("Heal Projectile")]
    [SerializeField] private GameObject healProjectilePrefab;
    [SerializeField] private Transform shootPoint;

    private float lastCastTime;
    private CharacterStats stats;
    private Animator animator;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        animator = GetComponent<Animator>();
    }

    public void Attack(ICombatTarget target)
    {
        var ally = target as CharacterStats;
        if (ally == null) return;
        if (healProjectilePrefab == null || shootPoint == null) return;
        if (stats == null || stats.currentHealth <= 0) return;

        if (Time.time - lastCastTime < stats.attackCooldown)
            return;

        lastCastTime = Time.time;

        animator?.SetTrigger("attack");

        GameObject projGO = Instantiate(healProjectilePrefab, shootPoint.position, Quaternion.identity);
        var proj = projGO.GetComponent<HealProjectile>();
        if (proj != null)
        {
            int healAmount = Mathf.Max(0, stats.damage); // HEAL
            proj.Init(stats, ally, healAmount);
        }
    }
}
