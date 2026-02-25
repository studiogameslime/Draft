using UnityEngine;

// Specialized lifesteal for enemy units like the Vampire.
public class EnemyLifeSteal : MonoBehaviour
{
    [Header("Settings")]
    [SerializeField] private float lifeStealPercent = 0.2f; // Default 20%

    private CharacterStats stats;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
    }

    private void Start()
    {
        // Subscribe to the damage event common to all CharacterStats
        if (stats != null)
        {
            stats.OnDealDamage += HandleDealDamage;
        }
    }

    private void HandleDealDamage(CharacterStats target, int damage)
    {
        if (stats == null || !stats.IsAlive) return;

        // Calculate heal amount based on damage dealt
        int heal = Mathf.RoundToInt(damage * lifeStealPercent);
        if (heal <= 0) return;

        // Apply heal and stay within max health limits
        stats.currentHealth = Mathf.Min(stats.currentHealth + heal, stats.maxHealth);

        // Use existing floating text system for feedback
        stats.showFloatingDamage(heal, FloatingNumberType.Heal);
    }

    private void OnDestroy()
    {
        // Unsubscribe to avoid memory leaks
        if (stats != null)
        {
            stats.OnDealDamage -= HandleDealDamage;
        }
    }
}