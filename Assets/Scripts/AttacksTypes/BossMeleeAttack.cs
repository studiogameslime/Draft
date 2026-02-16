using UnityEngine;

public class BossMeleeAttack : MonoBehaviour, IAttackStrategy
{
    private ICombatTarget currentTarget; 
    private CharacterStats stats;
    private Animator animator;

    [Header("Knockback")]
    [SerializeField] private float knockbackDistance = 0.8f;
    [SerializeField] private float knockbackDuration = 0.15f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        animator = GetComponent<Animator>();
    }

    public void Attack(ICombatTarget target)
    {
        if (target == null) return;

        currentTarget = target;

        if (animator != null)
            animator.SetTrigger("attack");
    }

    public void OnAttackHit()
    {
        if (currentTarget == null) return;

        currentTarget.TakeDamage(stats.damage, stats);

        var targetComponent = currentTarget as Component;
        if (targetComponent != null)
        {
            var unitAI = targetComponent.GetComponent<UnitAI>();
            if (unitAI != null)
            {
                Vector2 dir = (targetComponent.transform.position - transform.position).normalized;
                unitAI.ApplyKnockback(dir, knockbackDistance, knockbackDuration);
            }

            var fairyAI = targetComponent.GetComponent<FairyAI>();
            if (fairyAI != null)
            {
                Vector2 dir = (targetComponent.transform.position - transform.position).normalized;
                fairyAI.ApplyKnockback(dir, knockbackDistance, knockbackDuration);
            }
        }
    }
}
