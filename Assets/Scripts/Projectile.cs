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
        //transform.LookAt(_target.position);

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
