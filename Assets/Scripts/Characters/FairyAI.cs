using System.Linq;
using System.Collections.Generic;
using UnityEngine;

public class FairyAI : MonoBehaviour
{
    private Animator animator;
    private CharacterStats myStats;
    private CharacterStats targetAlly;

    private IAttackStrategy healStrategy;
    private float lastCastTime;

    [Header("Movement")]
    public float stoppingBuffer = 0.1f;

    [Header("Targeting")]
    [Range(0f, 1f)] public float switchOnPercent = 0.7f;   // switch if unit below this
    [Range(0f, 1f)] public float switchOffPercent = 0.9f;  // leave if unit over this

    private void Awake()
    {
        animator = GetComponent<Animator>();
        myStats = GetComponent<CharacterStats>();
        healStrategy = GetComponent<IAttackStrategy>(); // FairyHealAttack
    }

    private void OnEnable()
    {
        animator?.SetBool("isMoving", false);
        targetAlly = null;
        lastCastTime = 0f;
    }

    private void Update()
    {
        if (myStats == null || myStats.currentHealth <= 0)
        {
            StopMoving();
            return;
        }

        if (targetAlly == null || !targetAlly.IsAlive || IsFullHp(targetAlly))
            targetAlly = FindBestAllyToHeal();

        TrySwitchTargetByThresholds();

        if (targetAlly == null)
        {
            StopMoving();
            return;
        }

        float dist = Vector3.Distance(transform.position, targetAlly.transform.position);
        if (dist > myStats.attackRange - stoppingBuffer)
        {
            MoveToward(targetAlly.transform.position);
        }
        else
        {
            StopMoving();
            TryHeal();
        }
    }

    private void TryHeal()
    {
        if (healStrategy == null || targetAlly == null) return;

        if (Time.time - lastCastTime >= myStats.attackCooldown)
        {
            lastCastTime = Time.time;
            healStrategy.Attack(targetAlly);
        }
    }

    private CharacterStats FindBestAllyToHeal()
    {
        var allies = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None)
            .Where(s => s != null
                        && s.IsAlive
                        && !s.IsUntargetable
                        && s.team == myStats.team
                        && s != myStats
                        && s.currentHealth < s.maxHealth)
            .ToList();

        if (allies.Count == 0) return null;

        return allies
            .OrderBy(s => (float)s.currentHealth / Mathf.Max(1, s.maxHealth))
            .ThenBy(s => Vector3.Distance(transform.position, s.transform.position))
            .FirstOrDefault();
    }

    private void TrySwitchTargetByThresholds()
    {
        if (targetAlly == null) return;

        float curPct = HpPercent(targetAlly);
        if (curPct < switchOffPercent) return;

        var urgent = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None)
            .Where(s => s != null
                        && s.IsAlive
                        && !s.IsUntargetable
                        && s.team == myStats.team
                        && s != myStats
                        && s.currentHealth < s.maxHealth
                        && HpPercent(s) <= switchOnPercent)
            .OrderBy(s => HpPercent(s))
            .ThenBy(s => Vector3.Distance(transform.position, s.transform.position))
            .FirstOrDefault();

        if (urgent != null)
            targetAlly = urgent;
    }

    private void MoveToward(Vector3 pos)
    {
        Vector3 dir = (pos - transform.position).normalized;
        transform.position += dir * myStats.moveSpeed * Time.deltaTime;
        animator?.SetBool("isMoving", true);
    }

    private void StopMoving()
    {
        animator?.SetBool("isMoving", false);
    }

    private float HpPercent(CharacterStats s)
        => (s == null) ? 1f : (float)s.currentHealth / Mathf.Max(1, s.maxHealth);

    private bool IsFullHp(CharacterStats s)
        => s != null && s.currentHealth >= s.maxHealth;
}
