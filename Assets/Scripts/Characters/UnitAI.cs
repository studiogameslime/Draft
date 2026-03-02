using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class UnitAI : MonoBehaviour
{
    private Animator animator;
    private CharacterStats myStats;
    private CharacterStats targetStats;

    [Header("Knockback")]
    [SerializeField] private AnimationCurve knockbackCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    private bool isKnockbacked = false;
    private Coroutine knockbackRoutine;
    private bool isInterrupted;
    private float interruptUntil;

    [Header("Retargeting")]
    [SerializeField] private bool retargetToClosestContinuously = true;
    [SerializeField] private float retargetInterval = 0.25f;     // How often (in seconds) to re-evaluate the target
    [SerializeField] private float retargetMinGain = 0.25f;        // Minimum distance advantage (in units) required to switch targets
    private float nextRetargetTime = 0f; // Next time a retarget check is allowed



    public ICombatTarget target;
    public ICombatTarget CurrentCombatTarget => target;

    private float lastAttackTime;
    private IAttackStrategy attackStrategy;

    [Header("Movement")]
    public float stoppingBuffer = 0.1f;

    [Header("Shooting")]
    [SerializeField] private Transform shootPoint;
    private Vector3 shootPointDefaultLocalPos;

    [Header("Taunt State")]
    private CharacterStats activeTaunter;
    private float tauntEndTime;

    public WallHealth wall;
    public WallCombatTarget wallTarget;

    private bool wallLocked = false;

    [Header("Gate Routing")]
    [SerializeField] private float wallLineYOffset = 0f;
    [SerializeField] private float gateArriveDistance = 0.12f;

    private enum GateMoveState
    {
        None,
        ToEntry,
        ToExit
    }

    private GateMoveState gateState = GateMoveState.None;

    // CHANGED: keep a stable route for this unit
    private GateController currentGate = null;
    private Transform currentGateEntry = null;
    private Transform currentGateExit = null;
    private bool gateEntryNotified = false;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
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

    // CHANGED: important so we do not keep gate open if unit is disabled or destroyed
    private void OnDisable()
    {
        CleanupGateRouting();
    }

    public void LockInitialTargetAtBattleStart()
    {
        ClearTargetState();
    }

    private void ClearTargetState()
    {
        targetStats = null;
        target = null;
        wallLocked = false;

        // CHANGED
        CleanupGateRouting();
    }

    private void Update()
    {
        if (myStats == null || myStats.currentHealth <= 0)
        {
            StopMoving();
            return;
        }

        // HARD STOP: knockback / interrupt
        if (isKnockbacked || isInterrupted)
        {
            if (isInterrupted && Time.time >= interruptUntil)
                isInterrupted = false;

            animator?.SetBool("isMoving", false);
            return;
        }

        // ADDED FOR TAUNT: Force the unit to only chase/attack the taunter
        // This completely bypasses the wall logic and normal targeting
        if (activeTaunter != null && activeTaunter.IsAlive && Time.time < tauntEndTime)
        {
            HandleUnitTargetMovementAndAttack();
            return;
        }

        // Enemy logic: check if already locked on the wall
        if (myStats.team == Team.EnemyTeam && wall != null && wallTarget != null && wallLocked)
        {
            targetStats = null;
            target = wallTarget;

            UpdateFacingToTransform(wall.transform);

            float distToWall = DistanceToWallCollider();
            if (distToWall > myStats.attackRange - stoppingBuffer)
                MoveTowardWall();
            else
            {
                StopMoving();
                AttackWallIfInRange();
            }
            return;
        }

        // Enemy logic: general targeting
        if (myStats.team == Team.EnemyTeam && wall != null && wallTarget != null)
        {
            float distToWall = DistanceToWallCollider();

            // CHANGED: Only search for nearby player units if our priority is NOT the Wall
            // This allows units like the Gnoll (Wall priority) to ignore blockers.
            if (myStats.targetPriorityClass != UnitClass.Wall)
            {
                CharacterStats candidate = FindClosestTargetableEnemy();

                if (candidate != null)
                {
                    float distToUnit = Vector3.Distance(transform.position, candidate.transform.position);
                    // Standard behavior: attack closer units
                    if (distToUnit < distToWall)
                    {
                        SetUnitTarget(candidate);
                        HandleUnitTargetMovementAndAttack();
                        return;
                    }
                }
            }

            // If unit priority is Wall OR no unit blockers are closer, move to wall
            SetWallTargetSoft();
            UpdateFacingToTransform(wall.transform);

            if (distToWall > myStats.attackRange - stoppingBuffer)
            {
                MoveTowardWall();
            }
            else
            {
                StopMoving();
                AttackWallIfInRange();
            }
            return;
        }

        // Non-enemy normal targeting (Player Units)
        if (targetStats == null || targetStats.currentHealth <= 0 || targetStats.isUntargetable)
        {
            targetStats = FindClosestTargetableEnemy();
            if (targetStats == null)
            {
                StopMoving();
                return;
            }

            target = targetStats;

            // CHANGED: when target changes, stop any previous gate routing
            CleanupGateRouting();
        }

        MaybeRetargetToClosest();
        HandleUnitTargetMovementAndAttack();
    }

    private void HandleUnitTargetMovementAndAttack()
    {
        if (targetStats == null) return;

        // CHANGED: gate routing for player units only
        if (myStats.team != Team.EnemyTeam)
        {
            if (TryHandleGateRoutingToTarget(targetStats.transform))
                return;
        }

        UpdateFacingToTransform(targetStats.transform);

        float distance = Vector3.Distance(transform.position, targetStats.transform.position);
        if (distance > myStats.attackRange - stoppingBuffer)
            MoveTowardTarget();
        else
        {
            StopMoving();
            AttackCurrentTarget();
        }
    }

    public void ApplyTaunt(CharacterStats taunter, float duration)
    {
        if (taunter == null || !taunter.IsAlive) return;

        activeTaunter = taunter;
        tauntEndTime = Time.time + duration;

        // Force the unit to switch target to the taunter immediately
        SetUnitTarget(taunter);
        CleanupGateRouting();
    }

    // CHANGED: Gate routing that opens on entry and closes on exit
    private bool TryHandleGateRoutingToTarget(Transform finalTarget)
    {
        if (wall == null || finalTarget == null)
            return false;

        if (gateState == GateMoveState.None)
        {
            float wallY = wall.transform.position.y + wallLineYOffset;
            bool needCross = (transform.position.y < wallY && finalTarget.position.y > wallY);
            if (!needCross)
                return false;

            currentGate = GateRegistry.GetClosestGate(transform.position);
            if (currentGate == null)
                return false;

            Transform entry, exit;
            currentGate.GetOrCreatePathForUnit(transform, out entry, out exit);
            currentGateEntry = entry;
            currentGateExit = exit;

            if (currentGateEntry == null || currentGateExit == null)
            {
                CleanupGateRouting();
                return false;
            }

            gateState = GateMoveState.ToEntry;
            gateEntryNotified = false;
        }

        if (currentGate == null || currentGateEntry == null || currentGateExit == null)
        {
            CleanupGateRouting();
            return false;
        }

        if (gateState == GateMoveState.ToEntry)
        {
            UpdateFacingToTransform(currentGateEntry);
            MoveTowardPosition(currentGateEntry.position);

            if (Vector3.Distance(transform.position, currentGateEntry.position) <= gateArriveDistance)
            {
                if (!gateEntryNotified)
                {
                    currentGate.NotifyUnitReachedEntry(transform);
                    gateEntryNotified = true;
                }
                gateState = GateMoveState.ToExit;
            }
            return true;
        }

        if (gateState == GateMoveState.ToExit)
        {
            UpdateFacingToTransform(currentGateExit);
            MoveTowardPosition(currentGateExit.position);

            if (Vector3.Distance(transform.position, currentGateExit.position) <= gateArriveDistance)
            {
                currentGate.NotifyUnitReachedExit(transform);
                CleanupGateRouting();
            }
            return true;
        }

        return false;
    }


    // CHANGED: one cleanup function that also cancels if needed
    private void CleanupGateRouting()
    {
        if (currentGate != null)
        {
            // If we already entered but did not exit, this will decrement passersInside
            currentGate.CancelUnit(transform);
        }

        gateState = GateMoveState.None;
        currentGate = null;
        currentGateEntry = null;
        currentGateExit = null;
        gateEntryNotified = false;
    }

    private void SetUnitTarget(CharacterStats candidate)
    {
        wallLocked = false;
        targetStats = candidate;
        target = candidate;
    }

    private void SetWallTargetSoft()
    {
        targetStats = null;
        target = wallTarget;
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

        // If we have a target priority class, prefer enemies of that class
        UnitClass priority = myStats.targetPriorityClass;
        if (priority != UnitClass.None && priority != UnitClass.Wall)
        {
            var priorityTargets = enemies
                .Where(e => e.definition != null && e.definition.unitClass == priority)
                .OrderBy(e => Vector3.Distance(transform.position, e.transform.position))
                .ToList();

            if (priorityTargets.Count > 0)
                return priorityTargets.First();
        }

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

    private void MoveTowardPosition(Vector3 pos)
    {
        Vector3 direction = (pos - transform.position).normalized;
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
        transform.position += dir * myStats.moveSpeed * Time.deltaTime;

        animator?.SetBool("isMoving", true);
    }

    private void StopMoving()
    {
        animator?.SetBool("isMoving", false);
    }

    private void AttackCurrentTarget()
    {
        if (target == null) return;
        if (isInterrupted || isKnockbacked) return;

        if (Time.time - lastAttackTime >= myStats.attackCooldown)
        {
            lastAttackTime = Time.time;
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
        {

            return;
        }

        animator?.SetBool("isMoving", false);
        target = wallTarget;


        if (Time.time - lastAttackTime >= myStats.attackCooldown)
        {
            lastAttackTime = Time.time;
            wallLocked = true;
            attackStrategy?.Attack(wallTarget);
        }
    }
    public void ApplyKnockback(Vector2 direction, float distance, float duration)
    {
        if (!gameObject.activeInHierarchy)
            return;

        // HARD interrupt
        Interrupt(duration);

        if (knockbackRoutine != null)
            StopCoroutine(knockbackRoutine);

        knockbackRoutine = StartCoroutine(
            KnockbackRoutine(direction, distance, duration)
        );
    }


    private IEnumerator KnockbackRoutine(Vector2 direction, float distance, float duration)
    {
        isKnockbacked = true;

        Vector3 startPos = transform.position;
        Vector3 endPos = startPos + (Vector3)(direction.normalized * distance);

        float t = 0f;
        while (t < duration)
        {
            t += Time.deltaTime;
            float p = Mathf.Clamp01(t / duration);
            float eased = knockbackCurve.Evaluate(p);
            transform.position = Vector3.Lerp(startPos, endPos, eased);
            yield return null;
        }

        transform.position = endPos;
        isKnockbacked = false;
    }
    public bool IsInterrupted => isInterrupted;

    public void Interrupt(float seconds)
    {
        isInterrupted = true;
        interruptUntil = Mathf.Max(interruptUntil, Time.time + Mathf.Max(0f, seconds));
        animator?.SetBool("isMoving", false);
        lastAttackTime = Time.time;

        var cancelable = GetComponent<ICancelableAttack>();
        cancelable?.CancelAttack();
    }

    private void MaybeRetargetToClosest()
    {
        if (!(attackStrategy is MeleeAttack)) return;

        if (!retargetToClosestContinuously) return;
        if (Time.time < nextRetargetTime) return;
        nextRetargetTime = Time.time + retargetInterval;

        if (myStats == null) return;
        if (isInterrupted || isKnockbacked) return;

        // Ignore retargeting if currently taunted
        if (activeTaunter != null && activeTaunter.IsAlive && Time.time < tauntEndTime)
        {
            return;
        }

        CharacterStats closest = FindClosestTargetableEnemy();
        if (closest == null) return;

        if (targetStats == null)
        {
            SetUnitTarget(closest);
            CleanupGateRouting();
            return;
        }

        if (targetStats.currentHealth <= 0 || targetStats.isUntargetable)
        {
            SetUnitTarget(closest);
            CleanupGateRouting();
            return;
        }

        float currentDist = Vector3.Distance(transform.position, targetStats.transform.position);
        float newDist = Vector3.Distance(transform.position, closest.transform.position);

        if (closest != targetStats && newDist < currentDist - retargetMinGain)
        {
            SetUnitTarget(closest);
            CleanupGateRouting();
        }
    }


}
