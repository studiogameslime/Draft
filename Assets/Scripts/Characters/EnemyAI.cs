using UnityEngine;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    public float moveSpeed = 2f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;

    private Animator animator;
    private CharacterStats myStats;
    private CharacterStats targetStats;
    private float lastAttackTime;

    private IAttackStrategy attackStrategy;

    void Start()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
        myStats.lockedIn = true;

        attackStrategy = GetComponent<IAttackStrategy>();
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

        if (distance > attackRange)
        {
            MoveTowardTarget();
        }
        else
        {
            AttackTarget();
        }
    }

    void MoveTowardTarget()
    {
        Vector3 direction = (targetStats.transform.position - transform.position).normalized;
        transform.position += direction * moveSpeed * Time.deltaTime;
        animator.SetBool("isMoving", true);
    }

    void AttackTarget()
    {
        animator.SetBool("isMoving", false);

        if (Time.time - lastAttackTime > attackCooldown)
        {
            lastAttackTime = Time.time;
            attackStrategy?.Attack(targetStats);
        }
    }

    void FindClosestEnemy()
    {
        CharacterStats[] allCharacters = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        var enemies = allCharacters
            .Where(c => c.team != myStats.team && c.currentHealth > 0)
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
}
