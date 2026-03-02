using UnityEngine;

public class SkillMeteorSplash : UnitSkillBehaviour, IMeteorSkill
{
    private float damageFraction;
    private float radius;

    protected override void OnInit()
    {
        damageFraction = Mathf.Clamp01(node.skillPercentValue);
        radius = node.skillRadiusValue;
    }

    public void OnMeteorSpawned(MeteorProjectile meteor)
    {
        meteor.splashDamageFraction = damageFraction;
        meteor.splashRadius = radius;
    }
}
