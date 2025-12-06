using UnityEngine;
using UnityEngine.AI;
using System.Linq;

public class EnemyAI : MonoBehaviour
{
    private Animator animator;
    private CharacterStats myStats;
    private CharacterStats targetStats;
    private float lastAttackTime;
    private IAttackStrategy attackStrategy;
    private NavMeshAgent agent;

    [Header("NavMesh")]
    public float stoppingBuffer = 0.1f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
        attackStrategy = GetComponent<IAttackStrategy>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = myStats.moveSpeed;
            agent.acceleration = 100f;
            agent.angularSpeed = 720f;

            agent.stoppingDistance = Mathf.Max(0f, myStats.attackRange - stoppingBuffer);
        }
    }

    void OnEnable()
    {
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }
    }

    void OnDisable()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (animator != null)
            animator.SetBool("isMoving", false);
    }

    void Update()
    {
        if (myStats == null || myStats.currentHealth <= 0)
        {
            StopMoving();
            return;
        }

        FindClosestEnemy();
        if (targetStats == null)
        {
            StopMoving();
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
            StopMoving();
            AttackTarget();
        }
    }

    private void UpdateFacing()
    {
        if (targetStats == null) return;
        var sr = GetComponent<SpriteRenderer>();
        if (!sr) return;
        sr.flipX = targetStats.transform.position.x < transform.position.x;
    }

    void MoveTowardTarget()
    {
        if (!agent || !agent.enabled || !agent.isOnNavMesh)
        {
            // FALLBACK MOVEMENT
            Vector3 direction = (targetStats.transform.position - transform.position).normalized;
            transform.position += direction * myStats.moveSpeed * Time.deltaTime;
            animator.SetBool("isMoving", true);
            return;
        }

        agent.isStopped = false;
        agent.speed = myStats.moveSpeed;
        agent.SetDestination(targetStats.transform.position);

        bool moving = agent.velocity.sqrMagnitude > 0.01f;
        animator.SetBool("isMoving", moving);
    }

    void StopMoving()
    {
        if (agent && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        animator?.SetBool("isMoving", false);
    }

    void AttackTarget()
    {
        if (Time.time - lastAttackTime > myStats.attackCooldown)
        {
            lastAttackTime = Time.time;
            attackStrategy?.Attack(targetStats);
        }
    }

    void FindClosestEnemy()
    {
        var enemies = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None)
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
