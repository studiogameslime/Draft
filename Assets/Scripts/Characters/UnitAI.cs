using UnityEngine;
using System.Linq;
using System.Collections.Generic;

public class UnitAI : MonoBehaviour
{
    private Animator animator;
    private CharacterStats myStats;

    // Unit target (kept for compatibility)
    private CharacterStats targetStats;

    // Unified target (unit or wall)
    public ICombatTarget target;
    public ICombatTarget CurrentCombatTarget => target;

    private float lastAttackTime;
    private IAttackStrategy attackStrategy;

    [Header("Movement")]
    public float stoppingBuffer = 0.1f;

    [Header("Shooting")]
    [SerializeField] private Transform shootPoint;
    private Vector3 shootPointDefaultLocalPos;

    // Wall references
    public WallHealth wall;
    public WallCombatTarget wallTarget;

    // If true, this unit commits to the wall and stops searching for unit targets
    private bool wallLocked = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();

        // Must match: IAttackStrategy.Attack(ICombatTarget target)
        attackStrategy = GetComponent<IAttackStrategy>();

        if (shootPoint != null)
            shootPointDefaultLocalPos = shootPoint.localPosition;

        wall = FindFirstObjectByType<WallHealth>();
        wallTarget = FindFirstObjectByType<WallCombatTarget>();
    }

    private void OnEnable()
    {
        animator?.SetBool("isMoving", false);
        ClearTargetState();
        lastAttackTime = 0f;
    }

    // Call this when a unit is released into battle so it starts fresh
    public void LockInitialTargetAtBattleStart()
    {
        ClearTargetState();
    }

    private void ClearTargetState()
    {
        targetStats = null;
        target = null;
        wallLocked = false;
    }

    private void Update()
    {
        if (myStats == null || myStats.currentHealth <= 0)
        {
            StopMoving();
            return;
        }

        // If enemy already committed to wall, do not search for units anymore
        if (myStats.team == Team.EnemyTeam && wall != null && wallTarget != null && wallLocked)
        {
            targetStats = null;
            target = wallTarget;

            UpdateFacingToTransform(wall.transform);

            float distToWall = DistanceToWallCollider();
            if (distToWall > myStats.attackRange - stoppingBuffer)
            {

                MoveTowardWall();
            }
            else
            {

                StopMoving();
                AttackWallIfInRange(); // This keeps attacking, already locked
            }
            return;
        }

        // Enemy logic: keep searching units until the first real wall attack happens
        if (myStats.team == Team.EnemyTeam && wall != null && wallTarget != null)
        {

            CharacterStats candidate = FindClosestTargetableEnemy();
            float distToWall = DistanceToWallCollider();

            if (candidate != null)
            {
                float distToUnit = Vector3.Distance(transform.position, candidate.transform.position);

                // If a unit is closer than the wall, chase the unit
                if (distToUnit < distToWall)
                {
                    SetUnitTarget(candidate);
                    HandleUnitTargetMovementAndAttack();
                    return;
                }
            }

            // Otherwise move toward the wall, but do NOT lock yet
            SetWallTargetSoft();

            UpdateFacingToTransform(wall.transform);

            if (distToWall > myStats.attackRange - stoppingBuffer)
            {
                MoveTowardWall();
            }
            else
            {
                StopMoving();
                AttackWallIfInRange(); // This will lock only when an actual attack triggers
            }
            return;
        }

        // Non-enemy (or no wall): normal unit targeting
        if (targetStats == null || targetStats.currentHealth <= 0 || targetStats.isUntargetable)
        {

            targetStats = FindClosestTargetableEnemy();
            if (targetStats == null)
            {

                StopMoving();
                return;
            }
            target = targetStats;
        }

        HandleUnitTargetMovementAndAttack();
    }

    private void HandleUnitTargetMovementAndAttack()
    {
        if (targetStats == null) return;

        UpdateFacingToTransform(targetStats.transform);

        float distance = Vector3.Distance(transform.position, targetStats.transform.position);
        if (distance > myStats.attackRange - stoppingBuffer)
        {
            MoveTowardTarget();
        }
        else
        {
            StopMoving();
            AttackCurrentTarget();
        }
    }

    private void SetUnitTarget(CharacterStats candidate)
    {
        wallLocked = false;
        targetStats = candidate;
        target = candidate;
    }

    // Sets wall as current target without committing
    private void SetWallTargetSoft()
    {
        targetStats = null;
        target = wallTarget;
        // Do NOT set wallLocked here
    }

    private CharacterStats FindClosestTargetableEnemy()
    {
        if (myStats == null) return null;

        List<CharacterStats> enemies = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None)
            .Where(c => c != null
                        && c.team != myStats.team
                        && c.currentHealth > 0
                        && !c.isUntargetable)
            .ToList();

        if (enemies.Count == 0)
            return null;

        return enemies
            .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
            .First();
    }

    private void UpdateFacingToTransform(Transform t)
    {
        if (t == null) return;

        var sr = GetComponent<SpriteRenderer>();
        bool faceLeft = t.position.x < transform.position.x;

        if (sr != null)
            sr.flipX = faceLeft;

        if (shootPoint != null)
        {
            Vector3 lp = shootPointDefaultLocalPos;
            lp.x = Mathf.Abs(lp.x) * (faceLeft ? -1f : 1f);
            shootPoint.localPosition = lp;
        }
    }

    private void MoveTowardTarget()
    {
        if (targetStats == null) return;

        Vector3 direction = (targetStats.transform.position - transform.position).normalized;
        transform.position += direction * myStats.moveSpeed * Time.deltaTime;
        animator?.SetBool("isMoving", true);
    }

    private void MoveTowardWall()
    {
        if (wall == null) return;

        float distToWall = DistanceToWallCollider();
        if (distToWall <= myStats.attackRange - stoppingBuffer)
        {
            StopMoving();
            return;
        }

        Vector3 dir = (wall.transform.position - transform.position).normalized;
        Vector3 step = dir * myStats.moveSpeed * Time.deltaTime;
        //Debug.Log($"dir=({dir.x:F6},{dir.y:F6}) step=({step.x:F6},{step.y:F6}) dt={Time.deltaTime:F6} speed={myStats.moveSpeed:F3}");
        transform.position += dir * myStats.moveSpeed * Time.deltaTime;
        //Debug.Log($"transform.position {transform.position}");
        animator?.SetBool("isMoving", true);
    }

    private void StopMoving()
    {
        animator?.SetBool("isMoving", false);
    }

    private void AttackCurrentTarget()
    {
        if (target == null) return;

        if (Time.time - lastAttackTime >= myStats.attackCooldown)
        {
            lastAttackTime = Time.time;
            // Strategy triggers animation and hit logic
            attackStrategy?.Attack(target);
        }
    }

    private float DistanceToWallCollider()
    {
        if (wall == null) return Mathf.Infinity;

        Collider2D wallCollider = wall.GetComponent<Collider2D>();
        if (wallCollider == null)
            return Vector3.Distance(transform.position, wall.transform.position);

        Vector3 closestPoint = wallCollider.ClosestPoint(transform.position);
        return Vector3.Distance(transform.position, closestPoint);
    }

    private void AttackWallIfInRange()
    {
        if (wallTarget == null) return;

        float dist = DistanceToWallCollider();
        if (dist > myStats.attackRange)
            return;

        animator?.SetBool("isMoving", false);
        target = wallTarget;

        if (Time.time - lastAttackTime >= myStats.attackCooldown)
        {
            lastAttackTime = Time.time;

            // Commit to wall only when an actual wall attack happens
            wallLocked = true;

            // Always use strategy so ranged units shoot projectiles
            attackStrategy?.Attack(wallTarget);
        }
    }
}
