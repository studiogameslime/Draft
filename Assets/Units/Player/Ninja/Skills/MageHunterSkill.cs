using UnityEngine;

/// Tier 2 Skill: Changes the Ninja's targeting priority to focus on Mage-class enemies.
public class MageHunterSkill : UnitSkillBehaviour
{
    private UnitClass originalPriority;

    protected override void OnInit()
    {
        if (stats == null) return;

        originalPriority = stats.targetPriorityClass;
        stats.targetPriorityClass = UnitClass.Mage;
    }

    private void OnDestroy()
    {
        if (stats != null)
            stats.targetPriorityClass = originalPriority;
    }
}
