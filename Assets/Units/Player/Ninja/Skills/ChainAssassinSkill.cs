using System.Collections;
using UnityEngine;

/// Tier 1 Skill: On kill, the Ninja triggers a new stealth run towards the next target.
public class ChainAssassinSkill : UnitSkillBehaviour
{
    private NinjaStealthRun stealthRun;

    protected override void OnInit()
    {
        stealthRun = GetComponent<NinjaStealthRun>();
        if (stealthRun == null) return;

        stats.OnKillEnemy += HandleKillEnemy;
    }

    private void HandleKillEnemy(CharacterStats killed)
    {
        if (stealthRun == null) return;
        if (stats == null || !stats.IsAlive) return;

        // Wait one frame so UnitAI.Update() can acquire a new target
        StartCoroutine(ChainStealthNextFrame());
    }

    private IEnumerator ChainStealthNextFrame()
    {
        yield return null;

        if (stealthRun == null || stats == null || !stats.IsAlive) yield break;

        stealthRun.ResetStealthRun();
        stealthRun.DoInitialStealthRun();
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.OnKillEnemy -= HandleKillEnemy;
    }
}
