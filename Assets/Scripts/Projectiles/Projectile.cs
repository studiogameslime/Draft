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

    public void Init(CharacterStats attackerStats, ICombatTarget target)
    {
        _attackerStats = attackerStats;
        _target = target;

        _targetTransform = (_target != null) ? _target.TargetTransform : null;

        Vector3 startPos = transform.position;
        _flatPos = startPos;
        _time = 0f;
        _spinAngle = 0f;

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

        if (_target != null && _attackerStats != null && _target.IsAlive)
        {
            _target.TakeDamage(_attackerStats.damage, _attackerStats);

            // NEW: apply on-hit skills
            var effects = _attackerStats.GetComponents<IOnHitEffect>();
            foreach (var e in effects)
            {
                e.OnHit(_attackerStats, _target as CharacterStats);
            }
        }

        Destroy(gameObject);
    }

}
