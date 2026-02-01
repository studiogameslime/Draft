using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8f;
    public float arcHeight = 2f;

    [Header("Rotation Settings")]
    public bool rotate = false;
    public float rotationSpeed = 360f;

    [Header("Runtime")]
    private ICombatTarget _target;
    private Transform _targetTransform;
    private CharacterStats _attackerStats;

    private Vector3 _flatPos;
    private float _time;
    private float _travelDuration;
    private float _spinAngle;

    // NEW: damage multiplier (for falloff on ricochet, etc.)
    private float _damageMul = 1f;

    public void Init(CharacterStats attackerStats, ICombatTarget target)
    {
        _attackerStats = attackerStats;
        _target = target;
        _targetTransform = (_target != null) ? _target.TargetTransform : null;

        Vector3 startPos = transform.position;
        _flatPos = startPos;
        _time = 0f;
        _spinAngle = 0f;

        // NEW: reset multiplier per projectile spawn
        _damageMul = 1f;

        RecalculateTravelDuration(startPos);
    }

    // NEW: called by ricochet to reduce damage for next hit(s)
    public void SetDamageMultiplier(float mul)
    {
        _damageMul = Mathf.Max(0f, mul);
    }

    // NEW: called by ricochet to retarget the same projectile
    public void SetTarget(ICombatTarget newTarget)
    {
        _target = newTarget;
        _targetTransform = (_target != null) ? _target.TargetTransform : null;

        // reset flight from current position
        Vector3 startPos = transform.position;
        _flatPos = startPos;
        _time = 0f;
        _spinAngle = 0f;

        RecalculateTravelDuration(startPos);
    }

    private void RecalculateTravelDuration(Vector3 startPos)
    {
        if (_targetTransform != null)
        {
            float distance = Vector3.Distance(startPos, _targetTransform.position);
            _travelDuration = distance / Mathf.Max(0.01f, speed);
            if (_travelDuration <= 0f) _travelDuration = 0.1f;
        }
        else
        {
            _travelDuration = 0.5f;
        }
    }

    private void Update()
    {
        // Unity destroyed-object check through interface
        if (_target is UnityEngine.Object obj && obj == null)
        {
            Destroy(gameObject);
            return;
        }

        // Also guard transform reference
        if (_targetTransform == null)
        {
            Destroy(gameObject);
            return;
        }

        // If your target can become "not alive" without being destroyed
        if (_target != null && !_target.IsAlive)
        {
            Destroy(gameObject);
            return;
        }

        _time += Time.deltaTime;
        float t = Mathf.Clamp01(_time / _travelDuration);

        Vector3 targetPos = _targetTransform.position;
        Vector3 dir = (targetPos - _flatPos).normalized;
        _flatPos += dir * speed * Time.deltaTime;

        float height = 4f * arcHeight * t * (1f - t);
        Vector3 nextPos = _flatPos + Vector3.up * height;

        Vector3 moveDir = nextPos - transform.position;
        Quaternion baseRotation = transform.rotation;

        if (moveDir.sqrMagnitude > 0.0001f)
        {
            float angle = Mathf.Atan2(moveDir.y, moveDir.x) * Mathf.Rad2Deg;
            baseRotation = Quaternion.AngleAxis(angle, Vector3.forward);
        }

        if (rotate)
        {
            _spinAngle += rotationSpeed * Time.deltaTime;
            Quaternion spinRotation = Quaternion.AngleAxis(_spinAngle, Vector3.forward);
            transform.rotation = baseRotation * spinRotation;
        }
        else
        {
            transform.rotation = baseRotation;
        }

        transform.position = nextPos;

        if (Vector3.Distance(transform.position, targetPos) < 0.2f || t >= 1f)
            HitTarget();
    }

    private void HitTarget()
    {
        if (_target is UnityEngine.Object obj && obj == null)
        {
            Destroy(gameObject);
            return;
        }

        var hitStats = _target as CharacterStats;

        if (_target != null && _attackerStats != null && _target.IsAlive)
        {
            // NEW: apply damage multiplier (ricochet falloff)
            int dmg = Mathf.RoundToInt(_attackerStats.damage * _damageMul);
            _target.TakeDamage(dmg, _attackerStats);

            // apply on-hit skills (your existing system)
            var effects = _attackerStats.GetComponents<IOnHitEffect>();
            foreach (var e in effects)
                e.OnHit(_attackerStats, hitStats);

            // NEW: let projectile handlers (ricochet etc.) decide to continue
            var handlers = GetComponents<IProjectileOnHit>();
            foreach (var h in handlers)
            {
                if (h != null && h.TryHandleAfterHit(this, _attackerStats, hitStats))
                    return; // continue flying, DON'T destroy
            }
        }

        Destroy(gameObject);
    }
}
