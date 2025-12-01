using UnityEngine;

public class TankAttack : MonoBehaviour, IAttackStrategy
{
    private CharacterStats currentTarget;
    private CharacterStats stats;
    private Animator animator;
    

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        animator = GetComponent<Animator>(); // or GetComponentInChildren<Animator>() if needed
    }

    // Called by EnemyAI when we want to attack
    public void Attack(CharacterStats target)
    {
        if (target == null) return;

        // save target for the animation hit moment
        currentTarget = target;

        // start attack animation
        if (animator != null)
            animator.SetTrigger("attack");
    }

    // This function will be called from the animation event (in the middle of the attack)
    public void OnAttackHit()
    {
        if (currentTarget == null) return;

        // apply damage only at the hit frame
        currentTarget.TakeDamage(stats.damage);

        // optional: clear reference
        // currentTarget = null;
    }
}
