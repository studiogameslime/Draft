using System.Collections.Generic;
using UnityEngine;

public class LightningMageAttack : MonoBehaviour, IAttackStrategy
{
    [Header("Chain lightning settings")]
    public float chainRange = 3f;
    public int maxBounces = 1;
    public float secondaryDamageMultiplier = 0.5f;

    [Header("VFX")]
    public LightningBoltVfx boltPrefab;
    public Transform shootPoint;

    private float lastAttackTime;
    private ICombatTarget currentTarget;
    private CharacterStats stats;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
    }

    public void Attack(ICombatTarget target)
    {
        if (target == null || stats == null || !target.IsAlive) return;

        if (Time.time - lastAttackTime >= stats.attackCooldown)
        {
            lastAttackTime = Time.time;
            currentTarget = target;
            animator?.SetTrigger("attack");
        }
    }

    // Animation Event
    public void DoChainLightning()
    {
        if (currentTarget == null || stats == null || !currentTarget.IsAlive) return;

        List<ICombatTarget> chain = BuildTargetChain(currentTarget);
        if (chain.Count == 0) return;

        for (int i = 0; i < chain.Count; i++)
        {
            ICombatTarget t = chain[i];
            if (t == null || !t.IsAlive) continue;

            int dmg = (i == 0)
                ? stats.damage
                : Mathf.RoundToInt(stats.damage * secondaryDamageMultiplier);

            t.TakeDamage(dmg, stats);
        }

        SpawnLightningVfx(chain);
    }

    private List<ICombatTarget> BuildTargetChain(ICombatTarget firstTarget)
    {
        var result = new List<ICombatTarget>();
        var current = firstTarget;

        result.Add(current);

        for (int i = 0; i < maxBounces; i++)
        {
            ICombatTarget next = FindNextTarget(current.TargetTransform.position, result);
            if (next == null) break;

            result.Add(next);
            current = next;
        }

        return result;
    }

    private ICombatTarget FindNextTarget(Vector3 fromPos, List<ICombatTarget> alreadyHit)
    {
        var all = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);

        CharacterStats best = null;
        float bestDist = Mathf.Infinity;

        // היריב ביחס אלי
        Team enemyTeam = (stats.team == Team.MyTeam) ? Team.EnemyTeam : Team.MyTeam;

        foreach (var u in all)
        {
            if (u == null) continue;
            if (u.team != enemyTeam) continue;
            if (u.currentHealth <= 0) continue;         // זה בסדר פה כי u הוא CharacterStats
            if (u.isUntargetable) continue;
            if (alreadyHit.Contains(u)) continue;       // CharacterStats מממש ICombatTarget

            float d = Vector3.Distance(fromPos, u.transform.position);
            if (d < chainRange && d < bestDist)
            {
                best = u;
                bestDist = d;
            }
        }

        return best; // CharacterStats הוא ICombatTarget
    }

    private void SpawnLightningVfx(List<ICombatTarget> chain)
    {
        if (boltPrefab == null || chain == null || chain.Count == 0) return;

        Vector3 start = (shootPoint != null) ? shootPoint.position : transform.position;

        for (int i = 0; i < chain.Count; i++)
        {
            var t = chain[i];
            if (t == null || t.TargetTransform == null) continue;

            Vector3 end = t.TargetTransform.position;

            LightningBoltVfx bolt = Instantiate(boltPrefab);
            bolt.Play(start, end);

            start = end;
        }
    }
}
