using UnityEngine;

/// Tier 2 Skill: Changes the Ninja's targeting priority to focus on Melee-class enemies.
public class WarriorSlayerSkill : UnitSkillBehaviour
{
    private UnitClass originalPriority;

    protected override void OnInit()
    {
        if (stats == null) return;

        originalPriority = stats.targetPriorityClass;
        stats.targetPriorityClass = UnitClass.Melee;
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.targetPriorityClass = originalPriority;
    }
}
