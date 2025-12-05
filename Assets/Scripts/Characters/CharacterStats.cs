using UnityEngine;
using UnityEngine.UI;

public class CharacterStats : MonoBehaviour
{
    // --- Definition reference (base stats live here) ---
    [Header("Definition")]
    public UnitDefinition definition; // The unit type this instance was created from

    // --- Runtime Stats (after scaling by level) ---
    [Header("Runtime Stats")]
    [HideInInspector] public int level = 1;
    [HideInInspector] public int maxHealth;
    [HideInInspector] public int currentHealth;
    [HideInInspector] public int damage;
    [HideInInspector] public float moveSpeed;
    [HideInInspector] public float attackRange;
    [HideInInspector] public float attackCooldown;

    // --- Other info ---
    [HideInInspector] public Team team;
    [HideInInspector] public MonsterType monsterType;
    [HideInInspector] public bool lockedIn;

    // --- Components ---
    private Animator animator;
    private bool isDead = false;
    public Vector3 _initialPosition;

    [SerializeField] private Image _hpBar;

    // ====================================================
    // INIT
    // ====================================================
    private void Awake()
    {
        animator = GetComponent<Animator>();
    }

    /// <summary>
    /// Initialize this unit with team, definition, and level.
    /// </summary>
    public void Init(Team currentTeam, UnitDefinition def, int level)
    {
        definition = def;
        team = currentTeam;
        monsterType = def.monsterType;
        this.level = Mathf.Max(1, level);

        // Apply base stats scaled by level
        maxHealth = CalcScaledStat(def.maxHealth, 0.05f, this.level);
        currentHealth = maxHealth;
        damage = CalcScaledStat(def.damage, 0.05f, this.level);
        moveSpeed = def.moveSpeed;
        attackRange = def.attackRange;
        attackCooldown = def.attackCooldown;

        // Enemy visuals
        if (currentTeam == Team.EnemyTeam)
        {
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.flipX = true;

            _hpBar.color = Color.red;
        }
        else
        {
            _hpBar.color = Color.green;
        }

        UpdateHPBar();

    }

    //Save the unit initial position
    public void SetInitialPosition()
    {
        _initialPosition = transform.position;
    }

    // ====================================================
    // STAT CALC
    // ====================================================
    /// <summary>
    /// Returns baseValue * (1.05 ^ (level-1))
    /// Level 1 = 100%
    /// Level 2 = 105%
    /// Level 3 = 110.25%
    /// </summary>
    private int CalcScaledStat(int baseValue, float perLevelPercent, int level)
    {
        if (level <= 1) return baseValue;
        float factor = Mathf.Pow(1f + perLevelPercent, level - 1);
        return Mathf.RoundToInt(baseValue * factor);
    }

    // ====================================================
    // HP / DAMAGE
    // ====================================================
    public void TakeDamage(int amount)
    {
        if (isDead) return;

        currentHealth -= amount;
        UpdateHPBar();

        if (currentHealth <= 0)
            Die();
    }

    private void UpdateHPBar()
    {
        if (_hpBar != null && maxHealth > 0)
            _hpBar.fillAmount = (float)currentHealth / maxHealth;
    }

    // ====================================================
    // DEATH
    // ====================================================
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log($"{gameObject.name} died!");

        // Play death animation
        if (animator != null)
            animator.SetTrigger("dying");

        // Disable combat scripts
        DisableAllCombatScripts();

        // Stop movement
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Destroy(gameObject, 2f);
    }

    private void DisableAllCombatScripts()
    {
        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        var ranger = GetComponent<RangerAttack>();
        if (ranger != null) ranger.enabled = false;

        var tank = GetComponent<TankAttack>();
        if (tank != null) tank.enabled = false;
    }

    private void EnableAllCombatScripts()
    {
        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = true;

        var ranger = GetComponent<RangerAttack>();
        if (ranger != null) ranger.enabled = true;

        var tank = GetComponent<TankAttack>();
        if (tank != null) tank.enabled = true;
    }

    // ====================================================
    // LEVEL UP (OPTIONAL)
    // ====================================================
    public void SetLevel(int newLevel)
    {
        level = Mathf.Max(1, newLevel);

        maxHealth = CalcScaledStat(definition.maxHealth, 0.05f, level);
        damage = CalcScaledStat(definition.damage, 0.05f, level);

        // Keep current health within the new max HP
        currentHealth = Mathf.Min(currentHealth, maxHealth);

        UpdateHPBar();
    }

    // ====================================================
    // REVIVE
    // ====================================================
    public void Revive()
    {
        // Bring back to life
        isDead = false;

        // Restore stats
        currentHealth = maxHealth;
        UpdateHPBar();

        // Reset physics
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Re-enable combat scripts
        var ranger = GetComponent<RangerAttack>();
        if (ranger != null) ranger.enabled = true;

        var tank = GetComponent<TankAttack>();
        if (tank != null) tank.enabled = true;

        var ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        // Reset animator fully
        if (animator != null)
        {
            animator.ResetTrigger("dying");
            animator.ResetTrigger("attack");
            animator.SetBool("isMoving", false);

            animator.Rebind();
            animator.Update(0f);
        }

        // Reset sprite direction
        var sr = GetComponentInChildren<SpriteRenderer>();
        if (sr != null)
            sr.flipX = (team == Team.EnemyTeam);

        //Go back to initial position
        transform.position = _initialPosition;
    }

    public void Winning()
    {
        animator.SetTrigger("winning");
    }

}
