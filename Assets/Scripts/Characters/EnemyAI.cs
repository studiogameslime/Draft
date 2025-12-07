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
    public float stoppingBuffer = 0.1f;      // small buffer so agent stops slightly before attack range

    [Header("Line Of Sight")]
    public LayerMask obstaclesLayer;         // layer of static obstacles (igloos etc.)
    public float losCheckOffset = 0.3f;      // raycast a bit above ground

    [Header("Crowd / Around Target")]
    public float sideOffsetRadius = 0.8f;    // how much units spread around the target

    // cached offset so we do not change destination every frame
    private Vector3 cachedOffset;
    private CharacterStats lastTarget;

    void Awake()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
        attackStrategy = GetComponent<IAttackStrategy>();
        agent = GetComponent<NavMeshAgent>();

        if (agent != null && myStats != null)
        {
            agent.updateRotation = false;
            agent.updateUpAxis = false;
            agent.speed = myStats.moveSpeed;
            agent.acceleration = 100f;
            agent.angularSpeed = 720f;

            float stop = Mathf.Max(0f, myStats.attackRange - stoppingBuffer);
            agent.stoppingDistance = stop;
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

        // if target changed – choose a new side offset once
        if (targetStats != lastTarget)
        {
            lastTarget = targetStats;
            cachedOffset = GetSideOffset();
        }

        UpdateFacing();

        float distance = Vector3.Distance(transform.position, targetStats.transform.position);
        bool inRange = distance <= myStats.attackRange;
        bool hasLos = HasLineOfSight(targetStats);

        // movement rule:
        // if not in range OR no LOS - keep moving
        // only stop when in range AND has LOS
        if (!inRange || !hasLos)
        {
            MoveTowardsTargetWithOffset();
        }
        else
        {
            StopMoving();
            AttackTarget();
        }
    }

    // -------------------------------------------------
    // MOVEMENT
    // -------------------------------------------------
    void MoveTowardsTargetWithOffset()
    {
        if (targetStats == null)
            return;

        // final desired point around the target
        Vector3 targetPos = targetStats.transform.position + cachedOffset;

        if (agent == null || !agent.enabled || !agent.isOnNavMesh)
        {
            // fallback: simple movement without NavMesh
            Vector3 dir = (targetPos - transform.position).normalized;
            transform.position += dir * myStats.moveSpeed * Time.deltaTime;
            if (animator != null) animator.SetBool("isMoving", true);
            return;
        }

        agent.isStopped = false;
        agent.speed = myStats.moveSpeed;
        agent.SetDestination(targetPos);

        bool moving = agent.velocity.sqrMagnitude > 0.0001f;
        if (animator != null) animator.SetBool("isMoving", moving);
    }

    void StopMoving()
    {
        if (agent != null && agent.enabled)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
        }

        if (animator != null)
            animator.SetBool("isMoving", false);
    }

    // -------------------------------------------------
    // ATTACK
    // -------------------------------------------------
    void AttackTarget()
    {
        if (targetStats == null)
            return;

        if (Time.time - lastAttackTime > myStats.attackCooldown)
        {
            lastAttackTime = Time.time;
            attackStrategy?.Attack(targetStats);
        }
    }

    // -------------------------------------------------
    // LINE OF SIGHT
    // -------------------------------------------------
    bool HasLineOfSight(CharacterStats target)
    {
        if (target == null)
            return false;

        Vector3 start = transform.position + Vector3.up * losCheckOffset;
        Vector3 end = target.transform.position + Vector3.up * losCheckOffset;
        Vector3 dir = end - start;
        float dist = dir.magnitude;

        RaycastHit2D hit = Physics2D.Raycast(start, dir.normalized, dist, obstaclesLayer);

        // if ray hit nothing on obstacles layer - clear LOS
        return hit.collider == null;
    }

    // choose a stable side offset around the target
    Vector3 GetSideOffset()
    {
        // random direction on circle so units spread naturally
        Vector2 circle = Random.insideUnitCircle.normalized * sideOffsetRadius;
        return new Vector3(circle.x, circle.y, 0f);
    }

    // -------------------------------------------------
    // TARGETING + FACING
    // -------------------------------------------------
    void FindClosestEnemy()
    {
        var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        var enemies = all
            .Where(c => c.team != myStats.team && c.currentHealth > 0)
            .ToList();

        if (enemies.Count == 0)
        {
            targetStats = null;
            lastTarget = null;
            return;
        }

        targetStats = enemies
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .First();
    }

    void UpdateFacing()
    {
        if (targetStats == null)
            return;

        var sr = GetComponent<SpriteRenderer>();
        if (sr == null) return;

        sr.flipX = targetStats.transform.position.x < transform.position.x;
    }
}
