using System.Collections;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class CharacterStats : MonoBehaviour, ICombatTarget
{
    // --- Definition reference (base stats live here) ---
    [Header("Definition")]
    public UnitDefinition definition; // The unit type this instance was created from

    // --- Runtime Stats (after scaling by level) ---
    [Header("Runtime Stats")]
    public int level = 1;
    public int maxHealth;
    public int currentHealth;
    public int damage;
    public float moveSpeed;
    public float attackRange;
    public float attackCooldown;

    // --- Other info ---
    [HideInInspector] public Team team;
    [HideInInspector] public MonsterType monsterType;
    [HideInInspector] public bool lockedIn;
    [HideInInspector] public UnitClass unitClass;
    [HideInInspector] public bool isUntargetable = false;
    private CharacterStats lastAttacker;



    // --- Components ---
    private Animator animator;
    private bool isDead = false;
    public Vector3 _initialPosition;
    private SpriteRenderer spriteRenderer;
    public GameObject fallenWeaponPrefab;

    private float deathFadeDelay = 3f;
    private float deathFadeDuration = 2f;

    private Coroutine deathFadeRoutine;


    public Transform TargetTransform => transform;
    public bool IsAlive => currentHealth > 0;
    public bool IsUntargetable => isUntargetable;


    public GameObject floatingDamagePrefab;

    // ====================================================
    // INIT
    // ====================================================
    private void Awake()
    {
        animator = GetComponent<Animator>();
        spriteRenderer = GetComponent<SpriteRenderer>();
    }

    /// <summary>
    /// Initialize this unit with team, definition, and level.
    /// </summary>
    public void Init(Team currentTeam, UnitDefinition def, int level)
    {
        definition = def;
        team = currentTeam;
        unitClass = def.unitClass;
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
        }

    }

    // Save the unit initial position
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
    public void TakeDamage(int amount, CharacterStats attacker)
    {
        if (isDead) return;
        if (isUntargetable) return;

        if (attacker.definition.unitTeam == Team.MyTeam)
        {
            MissionsManager.Instance.ReportAction(MissionAction.DmgToEnemyUnits);
            MissionsManager.Instance.ReportAction(MissionAction.DmgToEnemyUnits, amount);
        }
        lastAttacker = attacker;


        GetComponent<HitFlash>()?.StartCoroutine("FlashWhite");
        currentHealth -= amount;


        showFloatingDamage();

        if (currentHealth <= 0)
        {
            if (attacker.definition.unitTeam == Team.MyTeam)
            {
                MissionsManager.Instance.ReportAction(MissionAction.KillEnemyUnits, 1);
                MissionsManager.Instance.ReportAction(MissionAction.KillSpecificEnemyUnit, 1, definition);
            }
            Die();
        }
    }
    //Floating damage text
    void showFloatingDamage()
    {
        if (floatingDamagePrefab)
        {
            var go = Instantiate(floatingDamagePrefab, transform.position, Quaternion.identity, transform);
            go.GetComponent<TextMeshPro>().text = currentHealth.ToString();
        }
    }


    // ====================================================
    // DEATH
    // ====================================================
    private void Die()
    {
        if (isDead) return;
        isDead = true;

        // Notify spawner (per-cell capacity system)
        GetComponent<SpawnedUnitOwnerLink>()?.NotifyOwnerDied();

        if (team == Team.MyTeam)
        {
            UnitCapacityManager.Instance.RegisterDeath(definition);
        }

        GetComponent<CircleCollider2D>().enabled = false;

        // Stop movement
        var rb = GetComponent<Rigidbody2D>();
        if (rb != null)
            rb.linearVelocity = Vector2.zero;

        // Play death animation
        if (animator != null)
            animator.SetTrigger("dying");

        // Disable combat scripts
        DisableAllCombatScripts();

        TrySpawnSoulOnDeath();

        if (deathFadeRoutine != null) StopCoroutine(deathFadeRoutine);
        deathFadeRoutine = StartCoroutine(FadeOutAfterDeath());
    }

    private void TrySpawnSoulOnDeath()
    {
        if (team != Team.EnemyTeam) return;
        if (definition == null) return;
        if (SoulOrbSpawner.instance == null) return;

        float chance = Mathf.Clamp01(definition.soulDropChance);
        if (chance <= 0f) return;

        if (Random.value <= chance)
        {
            Vector3 pos = transform.position + Vector3.up * 0.3f;
            SoulOrbSpawner.instance.SpawnSoul(pos, 1);
        }
    }

    private void DisableAllCombatScripts()
    {
        var ai = GetComponent<UnitAI>();
        if (ai != null) ai.enabled = false;

        var ranger = GetComponent<RangerAttack>();
        if (ranger != null) ranger.enabled = false;

        var tank = GetComponent<MeleeAttack>();
        if (tank != null) tank.enabled = false;
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
    }


    public void Winning()
    {
        animator.SetTrigger("winning");
    }

    public void SetAlpha(float newAlpha)
    {
        // 1. Get the current color
        Color currentColor = spriteRenderer.color;
        // 2. Set the new alpha value
        currentColor.a = newAlpha;
        // 3. Reassign the modified color back to the SpriteRenderer
        spriteRenderer.color = currentColor;
    }

    // ====================================================
    // FALLEN WEAPON (ANIMATED DROP, NO PHYSICS)
    // ====================================================
    public void SpawnFallenWeapon()
    {
        if (fallenWeaponPrefab == null)
        {
            return;
        }

        SetAlpha(0f);
        // spawn the weapon at the unit position
        var fallenWeapon = Instantiate(fallenWeaponPrefab, transform);



    }
    private IEnumerator FadeOutAfterDeath()
    {
        if (deathFadeRoutine != null)
            StopCoroutine(deathFadeRoutine);
        yield return new WaitForSeconds(deathFadeDelay);

        if (spriteRenderer == null)
            spriteRenderer = GetComponent<SpriteRenderer>();
        if (spriteRenderer == null)
            yield break;

        float startA = spriteRenderer.color.a;
        float t = 0f;

        while (t < deathFadeDuration)
        {
            t += Time.deltaTime;
            float a = Mathf.Lerp(startA, 0f, t / Mathf.Max(0.01f, deathFadeDuration));
            Color c = spriteRenderer.color;
            c.a = a;
            spriteRenderer.color = c;
            yield return null;
        }
        Destroy(gameObject);
    }
}
