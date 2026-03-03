using UnityEngine;

public interface ICancelableAttack
{
    void CancelAttack();
}

public class MeleeAttack : MonoBehaviour, IAttackStrategy, ICancelableAttack
{
    private ICombatTarget currentTarget;
    private CharacterStats stats;
    private Animator animator;


    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        animator = GetComponent<Animator>(); // or GetComponentInChildren<Animator>() if needed
    }

    // Called by EnemyAI when we want to attack
    public void Attack(ICombatTarget target)
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

        SoundManager.Instance?.PlaySFX(stats.definition.attackSound);

        // apply damage only at the hit frame
        currentTarget.TakeDamage(stats.damage, stats);

        // optional: clear reference
        // currentTarget = null;
    }

    public void CancelAttack()
    {
        currentTarget = null;
        if (animator != null)
        {
            animator.ResetTrigger("attack");
        }
    }

}
