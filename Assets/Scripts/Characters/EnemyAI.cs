using UnityEngine;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private Animator animator;
    private CharacterStats myStats;
    private CharacterStats targetStats;
    private float lastAttackTime;

    private IAttackStrategy attackStrategy;

    void Start()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
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

        UpdateFacing();

        float distance = Vector3.Distance(transform.position, targetStats.transform.position);

        if (distance > myStats.attackRange)
        {
            MoveTowardTarget();
        }
        else
        {
            AttackTarget();
        }
    }

    private void UpdateFacing()
    {
        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.flipX = targetStats.transform.position.x < transform.position.x;
    }

    void MoveTowardTarget()
    {
        Vector3 direction = (targetStats.transform.position - transform.position).normalized;
        transform.position += direction * myStats.moveSpeed * Time.deltaTime;
        animator.SetBool("isMoving", true);
    }

    void AttackTarget()
    {
        animator.SetBool("isMoving", false);

        if (Time.time - lastAttackTime > myStats.attackCooldown)
        {
            lastAttackTime = Time.time;

            // This only starts the animation now.
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
