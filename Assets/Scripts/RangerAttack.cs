using UnityEngine;

public class RangerAttack : MonoBehaviour, IAttackStrategy
{
    public GameObject projectilePrefab;
    public Transform shootPoint;

    private float lastAttackTime;
    private CharacterStats currentTarget;
    private CharacterStats stats;
    private Animator animator;


    private void Awake()
    {
        animator = GetComponent<Animator>(); // or GetComponentInChildren<Animator>() if needed
        stats = GetComponent<CharacterStats>();
    }

    // Called by EnemyAI when ranger should attack
    public void Attack(CharacterStats target)
    {
        if (target == null) return;

        if (Time.time - lastAttackTime >= stats.attackCooldown)
        {
            lastAttackTime = Time.time;

            // save target for the animation hit moment
            currentTarget = target;

            // start attack animation
            if (animator != null)
                animator.SetTrigger("attack");
        }
    }

    // Called from animation event (no parameters)
    public void StartProjectile()
    {
        if (currentTarget == null) return;
        if (projectilePrefab == null || shootPoint == null) return;

        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);

        var p = proj.GetComponent<Projectile>();
        if (p != null)
        {
            p.Init(stats,currentTarget.transform);
        }

        // optional: clear target after shot
        // currentTarget = null;
    }
}
