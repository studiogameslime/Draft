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
    private NavMeshObstacle obstacle;

    [Header("NavMesh")]
    public float stoppingBuffer = 0.1f;      // small buffer so agent stops slightly before attack range

    [Header("Line Of Sight")]
    public LayerMask obstaclesLayer;         // layer of static obstacles (igloos etc.)
    public float losCheckOffset = 0.3f;      // raycast a bit above ground

    [Header("Crowd / Around Target")]
    public float sideOffsetRadius = 0.8f;    // how much units spread around the target

    [Header("Agent / Obstacle Switch")]
    public bool useNavMeshObstacle = true;
    public float minMoveSpeedToBeMoving = 0.05f; // if agent speed < this, we treat as idle
    public float idleTimeToBecomeObstacle = 0.15f;

    // cached offset so we do not change destination every frame
    private Vector3 cachedOffset;
    private CharacterStats lastTarget;
    private float idleTimer = 0f;

    void Awake()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
        attackStrategy = GetComponent<IAttackStrategy>();
        agent = GetComponent<NavMeshAgent>();
        obstacle = GetComponent<NavMeshObstacle>();

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

        // ensure default state: agent on, obstacle off
        if (obstacle != null)
            obstacle.enabled = false;
    }

    void OnEnable()
    {
        if (agent != null)
        {
            agent.enabled = true;
            agent.isStopped = false;
            agent.velocity = Vector3.zero;
        }

        if (obstacle != null)
            obstacle.enabled = false;

        idleTimer = 0f;
    }

    void OnDisable()
    {
        if (agent != null)
        {
            agent.isStopped = true;
            agent.velocity = Vector3.zero;
            agent.enabled = false;
        }

        if (obstacle != null)
            obstacle.enabled = false;

        if (animator != null)
            animator.SetBool("isMoving", false);
    }

    void Update()
    {
        if (myStats == null || myStats.currentHealth <= 0)
        {
            StopMoving();
            UpdateAgentObstacleSwitch();
            return;
        }

        FindClosestEnemy();

        if (targetStats == null)
        {
            StopMoving();
            UpdateAgentObstacleSwitch();
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
        // if not in range OR no LOS -> keep moving
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

        UpdateAgentObstacleSwitch();
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

        if (agent == null)
        {
            // fallback: simple movement without NavMesh
            Vector3 dir = (targetPos - transform.position).normalized;
            transform.position += dir * myStats.moveSpeed * Time.deltaTime;
            if (animator != null) animator.SetBool("isMoving", true);
            return;
        }

        // if we are currently acting as an obstacle, switch back to agent
        if (useNavMeshObstacle && obstacle != null && obstacle.enabled)
        {
            obstacle.enabled = false;
            agent.enabled = true;
            agent.Warp(transform.position);
            idleTimer = 0f;
        }

        if (!agent.enabled || !agent.isOnNavMesh)
        {
            // fallback if something failed
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
    // AGENT / OBSTACLE SWITCH
    // -------------------------------------------------
    void UpdateAgentObstacleSwitch()
    {
        if (!useNavMeshObstacle || agent == null || obstacle == null)
            return;

        // if agent is disabled, we are already in obstacle mode – nothing to do
        if (!agent.enabled)
            return;

        float speed = agent.velocity.magnitude;

        if (speed > minMoveSpeedToBeMoving)
        {
            // moving - stay as agent
            idleTimer = 0f;
            if (obstacle.enabled)
                obstacle.enabled = false;
            return;
        }

        // not moving: count idle time
        idleTimer += Time.deltaTime;

        if (idleTimer >= idleTimeToBecomeObstacle)
        {
            // switch to obstacle mode
            agent.enabled = false;
            obstacle.enabled = true;
        }
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
