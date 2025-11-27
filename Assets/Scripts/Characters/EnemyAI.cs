using UnityEngine;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public int attackDamage = 10;

    private Animator animator;
    private CharacterStats myStats;
    private CharacterStats targetStats;

    private float lastAttackTime;

    void Start()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
    }

    void Update()
    {
        FindClosestEnemy();

        if (targetStats == null)
        {
            animator.SetBool("isMoving", false);
            return;
        }

        float distance = Vector3.Distance(transform.position, targetStats.transform.position);

        // if far, move to enemy
        if (distance > attackRange)
        {
            Vector3 direction = (targetStats.transform.position - transform.position).normalized;

            transform.position += direction * moveSpeed * Time.deltaTime;

            animator.SetBool("isMoving", true);
        }
        else // in attack range
        {
            animator.SetBool("isMoving", false);

            if (Time.time - lastAttackTime > attackCooldown)
            {
                Attack();
            }
        }
    }

    void FindClosestEnemy()
    {
        CharacterStats[] allCharacters = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        var enemies = allCharacters
            .Where(c => c.team != myStats.team)
            .Where(c => c.currentHealth > 0)
            .ToList();

        if (enemies.Count == 0)
        {
            targetStats = null;
            return;
        }

        targetStats = enemies
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .First();
    }

    void Attack()
    {
        animator.SetTrigger("attack");
        lastAttackTime = Time.time;

        if (targetStats != null)
        {
            targetStats.TakeDamage(attackDamage);
        }
    }
}
