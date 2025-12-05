using System.Collections.Generic;
using UnityEngine;

public class LightningMageAttack : MonoBehaviour, IAttackStrategy
{
    [Header("Chain lightning settings")]
    public float chainRange = 3f;          // max distance to jump from one enemy to the next
    public int maxBounces = 1;            // how many extra enemies after the first
    public float secondaryDamageMultiplier = 0.5f; // 0.5 = 50% damage on jumps


    [Header("VFX")]
    public LightningBoltVfx boltPrefab;   // prefab with LineRenderer effect
    public Transform shootPoint;          // where the lightning originates from (hand / staff)

    private float lastAttackTime;
    private CharacterStats currentTarget;
    private CharacterStats stats;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
    }

    /// <summary>
    /// Called by EnemyAI when this unit should attack a target.
    /// </summary>
    public void Attack(CharacterStats target)
    {
        if (target == null || stats == null)
            return;

        if (Time.time - lastAttackTime >= stats.attackCooldown)
        {
            lastAttackTime = Time.time;
            currentTarget = target;

            // Trigger attack animation
            if (animator != null)
                animator.SetTrigger("attack");
        }
    }

    /// <summary>
    /// Called from the attack animation event (no parameters).
    /// This is the moment when the lightning actually hits.
    /// </summary>
    public void DoChainLightning()
    {
        if (currentTarget == null || stats == null)
            return;

        List<CharacterStats> chain = BuildTargetChain(currentTarget);
        if (chain.Count == 0)
            return;

        // Apply damage
        for (int i = 0; i < chain.Count; i++)
        {
            CharacterStats t = chain[i];
            if (t == null || t.currentHealth <= 0)
                continue;

            // First target gets full damage, others get reduced damage
            int dmg = (i == 0)
                ? stats.damage
                : Mathf.RoundToInt(stats.damage * secondaryDamageMultiplier);

            t.TakeDamage(dmg);
        }

        // Spawn VFX
        SpawnLightningVfx(chain);
    }

    /// <summary>
    /// Creates a list of targets starting from the first target
    /// and bouncing to nearest enemies within chainRange.
    /// </summary>
    private List<CharacterStats> BuildTargetChain(CharacterStats firstTarget)
    {
        List<CharacterStats> result = new List<CharacterStats>();

        CharacterStats current = firstTarget;
        result.Add(current);

        for (int i = 0; i < maxBounces; i++)
        {
            CharacterStats next = FindNextTarget(current.transform.position, result);
            if (next == null)
                break;

            result.Add(next);
            current = next;
        }

        return result;
    }

    /// <summary>
    /// Finds the closest enemy that was not already hit and is within chainRange.
    /// </summary>
    private CharacterStats FindNextTarget(Vector3 fromPos, List<CharacterStats> alreadyHit)
    {
        CharacterStats[] all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        CharacterStats best = null;
        float bestDist = Mathf.Infinity;

        foreach (var u in all)
        {
            if (u == null) continue;
            if (u.team != Team.EnemyTeam) continue;
            if (u.currentHealth <= 0) continue;
            if (alreadyHit.Contains(u)) continue;

            float d = Vector3.Distance(fromPos, u.transform.position);
            if (d < chainRange && d < bestDist)
            {
                best = u;
                bestDist = d;
            }
        }

        return best;
    }

    /// <summary>
    /// Spawns lightning VFX segments from the mage to each target in the chain.
    /// </summary>
    private void SpawnLightningVfx(List<CharacterStats> chain)
    {
        if (boltPrefab == null || chain.Count == 0)
            return;

        // First segment starts from shootPoint (if defined) or from mage position
        Vector3 start = shootPoint != null ? shootPoint.position : transform.position;

        for (int i = 0; i < chain.Count; i++)
        {
            CharacterStats t = chain[i];
            if (t == null) continue;

            Vector3 end = t.transform.position;

            LightningBoltVfx bolt = Instantiate(boltPrefab);
            bolt.Play(start, end);

            // Next segment starts at this target
            start = end;
        }
    }
}
