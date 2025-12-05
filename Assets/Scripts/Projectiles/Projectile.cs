using UnityEngine;

public class Projectile : MonoBehaviour
{
    public float speed = 8f;
    public Transform _target;
    private CharacterStats _stats;


    public void Init(CharacterStats stats, Transform target)
    {
        _stats = stats;
        _target = target;
    }

    void Update()
    {
        if (_target == null)
        {
            Destroy(gameObject);
            return;
        }

        Vector3 dir = (_target.position - transform.position).normalized;
        transform.position += dir * speed * Time.deltaTime;
        float angle = Mathf.Atan2(dir.y, dir.x) * Mathf.Rad2Deg;
        transform.rotation = Quaternion.AngleAxis(angle, Vector3.forward);

        if (Vector3.Distance(transform.position, _target.position) < 0.2f)
        {
            HitTarget();
        }
    }

    void HitTarget()
    {
        CharacterStats stats = _target.GetComponent<CharacterStats>();
        if (stats != null)
            stats.TakeDamage(_stats.damage);

        Destroy(gameObject);
    }
}
