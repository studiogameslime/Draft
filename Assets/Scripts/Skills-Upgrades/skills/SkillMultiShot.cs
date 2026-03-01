using System.Collections;
using UnityEngine;

public class SkillMultiShot : UnitSkillBehaviour, IOnProjectileSpawned, IMeteorSkill
{
    private int extraShots;
    private float delayBetweenShots = 0.15f;
    private bool isSpawningExtra;

    protected override void OnInit()
    {
        extraShots = node.skillIntValue > 0 ? node.skillIntValue : 1;
    }

    // --- Archer (Projectile) ---

    public void OnProjectileSpawned(Projectile projectile)
    {
        if (isSpawningExtra || projectile == null) return;

        var rangerAttack = GetComponent<RangerAttack>();
        if (rangerAttack == null) return;

        StartCoroutine(SpawnExtraProjectiles(rangerAttack));
    }

    private IEnumerator SpawnExtraProjectiles(RangerAttack rangerAttack)
    {
        isSpawningExtra = true;
        for (int i = 0; i < extraShots; i++)
        {
            yield return new WaitForSeconds(delayBetweenShots);
            rangerAttack.StartProjectile();
        }
        isSpawningExtra = false;
    }

    // --- Fire Mage (Meteor) ---

    public void OnMeteorSpawned(MeteorProjectile meteor)
    {
        if (isSpawningExtra || meteor == null) return;

        var fireAttack = GetComponent<FireMageAttack>();
        if (fireAttack == null || fireAttack.meteorPrefab == null) return;

        ICombatTarget target = meteor.Target;
        if (target == null || !target.IsAlive) return;

        StartCoroutine(SpawnExtraMeteors(fireAttack, target));
    }

    private IEnumerator SpawnExtraMeteors(FireMageAttack fireAttack, ICombatTarget target)
    {
        isSpawningExtra = true;
        for (int i = 0; i < extraShots; i++)
        {
            yield return new WaitForSeconds(delayBetweenShots);

            if (target is Object obj && obj == null) break;
            if (!target.IsAlive) break;

            Vector3 targetPos = target.TargetTransform.position;
            Vector3 spawnPos = targetPos + Vector3.up * fireAttack.spawnHeight;
            spawnPos.x += Random.Range(-fireAttack.horizontalRandomOffset, fireAttack.horizontalRandomOffset);

            GameObject meteorObj = Instantiate(fireAttack.meteorPrefab, spawnPos, Quaternion.identity);
            MeteorProjectile proj = meteorObj.GetComponent<MeteorProjectile>();
            if (proj != null)
            {
                proj.Init(target, stats.damage, stats);

                var meteorSkills = GetComponents<IMeteorSkill>();
                foreach (var ms in meteorSkills)
                    ms.OnMeteorSpawned(proj);
            }
        }
        isSpawningExtra = false;
    }
}
