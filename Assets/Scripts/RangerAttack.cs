using UnityEngine;

public class RangerAttack : MonoBehaviour, IAttackStrategy
{
    public GameObject projectilePrefab;
    public Transform shootPoint;
    public float attackCooldown = 1f;
    private float lastAttackTime;

    public void Attack(CharacterStats target)
    {
        if (target == null) return;

        if (Time.time - lastAttackTime >= attackCooldown)
        {
            // Trigger animation
            GetComponent<Animator>().SetTrigger("attack");
            lastAttackTime = Time.time;

            // Create projectile (via Animation Event)
            StartProjectile(target);
        }
    }

    // This is called from the animation event
    public void StartProjectile(CharacterStats target)
    {
        GameObject proj = Instantiate(projectilePrefab, shootPoint.position, Quaternion.identity);
        proj.GetComponent<Projectile>().target = target.transform;
    }
}
