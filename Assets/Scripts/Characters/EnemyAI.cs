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

            if (myStats != null)
                agent.speed = myStats.moveSpeed;

            agent.acceleration = 100f;
            agent.angularSpeed = 720f;

            float stop = myStats != null
                ? Mathf.Max(0f, myStats.attackRange - stoppingBuffer)
                : agent.stoppingDistance;

            agent.stoppingDistance = stop;
        }
    }

    private void OnEnable()
    {
        if (agent != null)
        {
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }
    }

    private void OnDisable()
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

        if (agent != null)
            Debug.Log("OnNavMesh? " + agent.isOnNavMesh);

        FindClosestEnemy();

        if (targetStats == null || targetStats.currentHealth <= 0)
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
        if (sr == null) return;

        sr.flipX = targetStats.transform.position.x < transform.position.x;
    }

    void MoveTowardTarget()
    {
        if (targetStats == null) return;

        bool canUseNavMesh = agent != null && agent.isOnNavMesh;

        if (canUseNavMesh)
        {
            agent.isStopped = false;
            agent.speed = myStats.moveSpeed;

            agent.SetDestination(targetStats.transform.position);

            bool isMoving = agent.velocity.sqrMagnitude > 0.0001f;
            animator.SetBool("isMoving", isMoving);
        }
        else
        {
            Vector3 direction = (targetStats.transform.position - transform.position).normalized;
            transform.position += direction * myStats.moveSpeed * Time.deltaTime;

            animator.SetBool("isMoving", true);
        }
    }

    void StopMoving()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (animator != null)
            animator.SetBool("isMoving", false);
    }

    void AttackTarget()
    {
        if (targetStats == null) return;

        if (Time.time - lastAttackTime > myStats.attackCooldown)
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
