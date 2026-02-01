using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class ProjectileRicochet : MonoBehaviour, IProjectileOnHit
{
    private int remainingBounces;
    private float radius;
    private float falloff;          
    private float currentMul = 1f;

    private readonly HashSet<CharacterStats> hit = new HashSet<CharacterStats>();

    public void Init(int bounces, float radius, float falloff)
    {
        remainingBounces = Mathf.Max(0, bounces);
        this.radius = Mathf.Max(0.1f, radius);
        this.falloff = Mathf.Clamp(falloff, 0f, 1f);
        currentMul = 1f;
    }

    public bool TryHandleAfterHit(Projectile p, CharacterStats attacker, CharacterStats hitTarget)
    {
        if (p == null || attacker == null || hitTarget == null)
            return false;

        hit.Add(hitTarget);

        if (remainingBounces <= 0)
            return false;

        // מחפשים אויב חדש ליד מי שנפגע עכשיו
        var next = FindNextTarget(attacker, hitTarget.transform.position);
        if (next == null)
            return false;

        remainingBounces--;

        currentMul *= falloff;
        p.SetDamageMultiplier(currentMul);

        p.SetTarget(next);

        return true; 
    }

    private CharacterStats FindNextTarget(CharacterStats attacker, Vector3 fromPos)
    {
        var enemies = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None)
            .Where(s => s != null
                        && s.IsAlive
                        && !s.IsUntargetable
                        && s.team != attacker.team
                        && !hit.Contains(s))
            .Select(s => new { s, d = Vector3.Distance(fromPos, s.transform.position) })
            .Where(x => x.d <= radius)
            .OrderBy(x => x.d)
            .FirstOrDefault();

        return enemies?.s;
    }
}
