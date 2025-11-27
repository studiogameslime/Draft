using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public int attackDamage = 10;

    private NavMeshAgent agent;
    private Animator animator;
    private CharacterStats myStats;
    private CharacterStats targetStats;

    private float lastAttackTime;

    void Start()
    {
        agent = GetComponent<NavMeshAgent>();
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
    }

    void Update()
    {
        FindClosestEnemy();

        if (targetStats == null) return;

        float distance = Vector3.Distance(transform.position, targetStats.transform.position);

        if (distance > attackRange)
        {
            agent.SetDestination(targetStats.transform.position);
            animator.SetBool("isMoving", true);
        }
        else
        {
            agent.SetDestination(transform.position);
            animator.SetBool("isMoving", false);

            if (Time.time - lastAttackTime > attackCooldown)
            {
                Attack();
            }
        }
    }

    void FindClosestEnemy()
    {
        CharacterStats[] allCharacters = FindObjectsOfType<CharacterStats>();

        var enemies = allCharacters
            .Where(c => c.team != myStats.team) // not attacking my team
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
            targetStats.TakeDamage(attackDamage);
    }
}
