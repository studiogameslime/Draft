using UnityEngine;

public class SkillRicochet : UnitSkillBehaviour, IOnProjectileSpawned
{
    private int bounces;
    private float radius;
    private float falloff;

    protected override void OnInit()
    {
        bounces = Mathf.Max(0, node.skillIntValue);
        radius = Mathf.Max(0.1f, node.skillRadiusValue);
        falloff = Mathf.Clamp(node.skillPercentValue, 0f, 1f);
    }

    public void OnProjectileSpawned(Projectile projectile)
    {
        if (projectile == null) return;

        var ric = projectile.GetComponent<ProjectileRicochet>();
        if (ric == null) ric = projectile.gameObject.AddComponent<ProjectileRicochet>();

        ric.Init(bounces, radius, falloff);
    }
}
