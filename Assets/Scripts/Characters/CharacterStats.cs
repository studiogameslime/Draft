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
    [HideInInspector] public float baseMoveSpeed;


    [Header("Critical Hit Stats")]
    public float critChance;
    public float critMultiplier;
    public Color critColor;


    [Header("Floating Damage Layout")]
    public float floatingXRandom = 0.15f;
    private int floatingDamageOrder = 100;

    // --- Skill events ---
    public System.Action<CharacterStats, int> OnDealDamage;
    public System.Action<CharacterStats, int> OnTakeDamage;
    public System.Action<CharacterStats> OnKillEnemy;


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

        if (floatingDamagePrefab == null && GameAssetsProvider.Instance != null && GameAssetsProvider.Instance.Assets != null)
        {
            floatingDamagePrefab = GameAssetsProvider.Instance.Assets.floatingDamagePrefab;
        }
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

        maxHealth = CalcFinalIntStat(def.maxHealth, MasteryStat.GlobalHpPercent);
        damage = CalcFinalIntStat(def.damage, MasteryStat.GlobalDamagePercent);

        // NEW: unit level scaling (HP+DMG)
        float mul = UnitLevelingService.GetHpDamageMultiplier(this.level);
        maxHealth = Mathf.RoundToInt(maxHealth * mul);
        currentHealth = maxHealth;

        damage = Mathf.RoundToInt(damage * mul);
        moveSpeed = CalcFinalFloatStat(def.moveSpeed, MasteryStat.MoveSpeedPercent);
        baseMoveSpeed = moveSpeed;
        attackRange = def.attackRange;
        attackCooldown = def.attackCooldown;
        // Crit stats from definition
        critChance = def.critChance;
        critMultiplier = def.critMultiplier;
        critColor = def.critColor;


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

    private int CalcFinalIntStat(int baseValue, MasteryStat percentStat, MasteryStat? flatStat = null)
    {
        float v = baseValue;

        // 1) Flat first
        if (flatStat.HasValue && MasteryBonusManager.Instance != null)
            v += MasteryBonusManager.Instance.GetFlat(flatStat.Value);

        // 2) Percent after (stored as fraction: 0.08 = 8%)
        float p = 0f;
        if (MasteryBonusManager.Instance != null)
            p = MasteryBonusManager.Instance.GetPercent(percentStat);

        v *= (1f + p);

        return Mathf.RoundToInt(v);
    }

    private float CalcFinalFloatStat(float baseValue, MasteryStat percentStat)
    {
        float p = MasteryBonusManager.Instance != null
            ? MasteryBonusManager.Instance.GetPercent(percentStat)
            : 0f;

        return baseValue * (1f + p);
    }



    // ====================================================
    // HP / DAMAGE
    // ====================================================
    public void TakeDamage(int amount, CharacterStats attacker)
    {
        if (isDead || isUntargetable) return;

        bool isCrit = Random.value <= attacker.critChance;

        if (isCrit)
            amount = Mathf.RoundToInt(amount * attacker.critMultiplier);

        if (attacker.definition.unitTeam == Team.MyTeam)
        {
            MissionsManager.Instance.ReportAction(MissionAction.DmgToEnemyUnits);
            MissionsManager.Instance.ReportAction(MissionAction.DmgToEnemyUnits, amount);
        }

        lastAttacker = attacker;

        GetComponent<HitFlash>()?.StartCoroutine("FlashWhite");

        currentHealth -= amount;

        OnTakeDamage?.Invoke(attacker, amount);
        attacker?.OnDealDamage?.Invoke(this, amount);

        showFloatingDamage(amount, isCrit?FloatingNumberType.Crit:FloatingNumberType.Normal);

        if (currentHealth <= 0)
        {
            if (attacker.definition.unitTeam == Team.MyTeam)
            {
                MissionsManager.Instance.ReportAction(MissionAction.KillEnemyUnits, 1);
                MissionsManager.Instance.ReportAction(MissionAction.KillSpecificEnemyUnit, 1, definition);
            }
            attacker?.OnKillEnemy?.Invoke(this);
            Die();
        }
    }

    //Floating damage text
    public void showFloatingDamage(int amount, FloatingNumberType floatingNumberType)
    {
        if (!floatingDamagePrefab) return;

        // Random horizontal offset to avoid overlap
        Vector3 offset = Vector3.zero;
        offset.x = Random.Range(-floatingXRandom, floatingXRandom);

        // Instantiate the prefab at the enemy position + offset
        var go = Instantiate(
            floatingDamagePrefab,
            transform.position + offset,
            Quaternion.identity,
            transform
        );

        // Set the text
        var tmp = go.GetComponent<TextMeshPro>();
        tmp.text = amount.ToString();

        // Crit color

        switch (floatingNumberType)
        {
            case FloatingNumberType.Normal:
                {
                    tmp.color = Color.white;
                    break;
                }
            case FloatingNumberType.Crit:
                {
                    tmp.color = critColor;
                    break;
                }
            case FloatingNumberType.Heal:
                {
                    tmp.color = Color.green;
                    break;
                }
        }


        // Make sure the whole prefab renders on top by using Sorting Group
        var sg = go.GetComponent<UnityEngine.Rendering.SortingGroup>();
        if (sg != null)
        {
            sg.sortingOrder = floatingDamageOrder++;

            // Optional safety reset to avoid huge numbers
            if (floatingDamageOrder > 1000)
                floatingDamageOrder = 100;
        }

        // Destroy after some time
        Destroy(go, 1.2f);
    }


    public void Heal(int amount, CharacterStats healer = null)
    {
        if (amount <= 0) return;
        if (isDead) return;
        if (currentHealth <= 0) return;

        int before = currentHealth;
        currentHealth = Mathf.Min(currentHealth + amount, maxHealth);

        showFloatingDamage(amount, FloatingNumberType.Heal);
        // in the future if event needed:
        // OnReceiveHeal?.Invoke(healer, currentHealth - before);
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

    public void ApplyStatusDamage(int amount, CharacterStats attacker)
    {
        Debug.Log("ApplyStatusDamage");
        if (amount <= 0) return;
        if (isUntargetable || currentHealth <= 0) return;

        currentHealth -= amount;

        OnTakeDamage?.Invoke(attacker, amount);
        attacker?.OnDealDamage?.Invoke(this, amount);

        if (currentHealth <= 0)
        {
            attacker?.OnKillEnemy?.Invoke(this);
            DieFromStatus(attacker);
        }
    }

    private void DieFromStatus(CharacterStats attacker)
    {
        Die();
    }

}
