using UnityEngine;

public class FireMageAttack : MonoBehaviour, IAttackStrategy
{
    [Header("Meteor settings")]
    public GameObject meteorPrefab;               // meteor projectile prefab
    public float spawnHeight = 5f;                // how high above the target the meteor starts
    public float horizontalRandomOffset = 0.5f;   // small random x, so meteors are not 100% identical

    private float lastAttackTime;
    private CharacterStats currentTarget;
    private CharacterStats stats;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
    }

    // Called by EnemyAI when this unit should attack
    public void Attack(CharacterStats target)
    {
        if (target == null || stats == null)
            return;

        // Respect attack cooldown
        if (Time.time - lastAttackTime >= stats.attackCooldown)
        {
            lastAttackTime = Time.time;
            currentTarget = target;

            if (animator != null)
                animator.SetTrigger("attack");
        }
    }

    // Called from attack animation event (no parameters)
    // This is the moment when the meteor should spawn
    public void SpawnMeteor()
    {
        if (currentTarget == null || meteorPrefab == null || stats == null)
            return;

        // Target position on the ground
        Vector3 targetPos = currentTarget.transform.position;

        // Spawn above the target
        Vector3 spawnPos = targetPos + Vector3.up * spawnHeight;
        spawnPos.x += Random.Range(-horizontalRandomOffset, horizontalRandomOffset);

        // Instantiate meteor
        GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);

        // Initialize meteor logic
        MeteorProjectile proj = meteor.GetComponent<MeteorProjectile>();
        if (proj != null)
        {
            // Pass attacker team so splash will only damage enemies
            proj.Init(currentTarget, stats.damage, stats.team);
        }
    }
}
