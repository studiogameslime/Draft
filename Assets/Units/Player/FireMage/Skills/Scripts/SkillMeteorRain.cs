using UnityEngine;

public class SkillMeteorRain : UnitSkillBehaviour
{
    private float cooldown;
    private float timer;

    protected override void OnInit()
    {
        cooldown = node.skillDuration > 0f ? node.skillDuration : 5f;
        timer = cooldown;

        var fireAttack = GetComponent<FireMageAttack>();
        if (fireAttack == null)
            Debug.LogWarning("SkillMeteorRain: FireMageAttack not found on " + gameObject.name);
        else if (fireAttack.meteorPrefab == null)
            Debug.LogWarning("SkillMeteorRain: meteorPrefab is not assigned on " + gameObject.name);
    }

    private void Update()
    {
        if (stats == null || stats.currentHealth <= 0) return;
        if (BattleManager.instance == null || !BattleManager.instance.IsBattleRunning) return;
        if (stats.isUntargetable) return;

        timer -= Time.deltaTime;
        if (timer <= 0f)
        {
            timer = cooldown;
            FireMeteor();
        }
    }

    private void FireMeteor()
    {
        var fireAttack = GetComponent<FireMageAttack>();
        if (fireAttack == null || fireAttack.meteorPrefab == null) return;

        CharacterStats target = FindClosestEnemy();
        if (target == null) return;

        Vector3 targetPos = target.transform.position;
        Vector3 spawnPos = targetPos + Vector3.up * fireAttack.spawnHeight;
        spawnPos.x += Random.Range(-fireAttack.horizontalRandomOffset, fireAttack.horizontalRandomOffset);

        GameObject meteor = Instantiate(fireAttack.meteorPrefab, spawnPos, Quaternion.identity);
        MeteorProjectile proj = meteor.GetComponent<MeteorProjectile>();
        if (proj == null)
        {
            Debug.LogError("SkillMeteorRain: meteorPrefab has no MeteorProjectile component! Assign the correct prefab.");
            Destroy(meteor);
            return;
        }

        proj.Init(target, stats.damage, stats);

        var meteorSkills = GetComponents<IMeteorSkill>();
        foreach (var ms in meteorSkills)
            ms.OnMeteorSpawned(proj);
    }

    private CharacterStats FindClosestEnemy()
    {
        var allUnits = FindObjectsByType<CharacterStats>(FindObjectsSortMode.None);
        CharacterStats closest = null;
        float closestDist = float.MaxValue;

        foreach (var unit in allUnits)
        {
            if (unit == null || unit == stats || !unit.IsAlive) continue;
            if (unit.team == stats.team) continue;
            if (unit.isUntargetable) continue;

            float dist = Vector3.Distance(transform.position, unit.transform.position);
            if (dist < closestDist)
            {
                closestDist = dist;
                closest = unit;
            }
        }

        return closest;
    }
}
