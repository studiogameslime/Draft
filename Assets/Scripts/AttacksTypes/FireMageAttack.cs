using UnityEngine;

public class FireMageAttack : MonoBehaviour, IAttackStrategy
{
    [Header("Projectile (base attack)")]
    public GameObject projectilePrefab;
    public Transform shootPoint;

    [Header("Meteor (unlocked by skill)")]
    public GameObject meteorPrefab;
    public float spawnHeight = 5f;
    public float horizontalRandomOffset = 0.5f;

    private float lastAttackTime;
    private ICombatTarget currentTarget;
    private CharacterStats stats;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
    }

    public void Attack(ICombatTarget target)
    {
        if (target == null || stats == null || !target.IsAlive) return;

        if (Time.time - lastAttackTime >= stats.attackCooldown)
        {
            lastAttackTime = Time.time;
            currentTarget = target;
            animator?.SetTrigger("attack");
        }
    }

    public void StartProjectile()
    {
        if (currentTarget == null || !currentTarget.IsAlive) return;
        if (projectilePrefab == null || shootPoint == null || stats == null) return;

        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

        var p = proj.GetComponent<Projectile>();
        if (p != null)
            p.Init(stats, currentTarget);

        var spawnHooks = GetComponents<IOnProjectileSpawned>();
        for (int i = 0; i < spawnHooks.Length; i++)
            spawnHooks[i].OnProjectileSpawned(p);
    }
}
