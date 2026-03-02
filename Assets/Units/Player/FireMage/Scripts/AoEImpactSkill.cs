using UnityEngine;
using System.Collections.Generic;

// This script implements a reusable AoE Cleave ability.
// It syncs with the unit's internal attack speed and includes a recursion guard.
public class AoEImpactSkill : MonoBehaviour
{
    private CharacterStats myStats;

    [Header("Skill Settings")]
    [SerializeField] private float effectRadius = 2.0f;
    [SerializeField] private LayerMask targetLayer;

    private float lastExecutionTime;
    private bool isExecuting = false; // Guard to prevent infinite damage loops

    private void Awake()
    {
        myStats = GetComponent<CharacterStats>();
    }

    private void OnEnable()
    {
        if (myStats != null)
        {
            // Subscribing to the built-in event in CharacterStats.cs [2]
            myStats.OnDealDamage += HandleAoEEffect;
        }
    }

    private void OnDisable()
    {
        if (myStats != null)
        {
            myStats.OnDealDamage -= HandleAoEEffect;
        }
    }

    private void HandleAoEEffect(CharacterStats mainTarget, int damageDealt)
    {
        // RECURSION GUARD: Prevents AoE damage from triggering another AoE cycle
        if (isExecuting) return;

        // SYNC COOLDOWN: Uses the character's base attack speed from stats [2, 4]
        if (Time.time - lastExecutionTime < myStats.attackCooldown) return;

        isExecuting = true;
        lastExecutionTime = Time.time;

        // Find nearby enemies [5]
        Collider2D[] hits = Physics2D.OverlapCircleAll(transform.position, effectRadius, targetLayer);

        foreach (var hit in hits)
        {
            CharacterStats secondaryTarget = hit.GetComponent<CharacterStats>();

            // Validations: check team and ensure target is alive [6, 7]
            if (secondaryTarget != null && secondaryTarget != mainTarget && secondaryTarget.IsAlive)
            {
                if (secondaryTarget.team != myStats.team)
                {
                    // Calculate damage based on character stats [8]
                    int aoeDamage = Mathf.RoundToInt(myStats.damage);

                    // Apply damage through the standard system [9]
                    secondaryTarget.TakeDamage(aoeDamage, myStats);
                }
            }
        }

        // Release the guard after the loop finishes
        isExecuting = false;
    }

    private void OnDrawGizmosSelected()
    {
        Gizmos.color = Color.cyan;
        Gizmos.DrawWireSphere(transform.position, effectRadius);
    }
}