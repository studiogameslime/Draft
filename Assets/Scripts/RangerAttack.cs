using UnityEngine;

public class RangerAttack : MonoBehaviour, IAttackStrategy
{
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float attackCooldown = 1f;

    private float lastAttackTime;
    private CharacterStats currentTarget;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>(); // or GetComponentInChildren<Animator>() if needed
    }

    // Called by EnemyAI when ranger should attack
    public void Attack(CharacterStats target)
    {
        if (target == null) return;

        if (Time.time - lastAttackTime >= attackCooldown)
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
            p.target = currentTarget.transform;
        }

        // optional: clear target after shot
        // currentTarget = null;
    }
}
