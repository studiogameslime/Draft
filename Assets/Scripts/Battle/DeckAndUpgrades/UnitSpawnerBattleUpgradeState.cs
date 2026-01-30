using System.Collections.Generic;
using UnityEngine;

public class UnitSpawnerBattleUpgradeState : MonoBehaviour
{
    public int currentTier = 1;

    // Fast lookup
    public HashSet<string> pickedNodeIds = new HashSet<string>();

    // Keeps order for UI + applying
    public List<UnitUpgradeNodeDefinition> pickedNodes = new List<UnitUpgradeNodeDefinition>();

    public List<string> activeSkillEffectIds = new List<string>();

    public void Pick(UnitUpgradeNodeDefinition node)
    {
        if (node == null) return;
        if (string.IsNullOrEmpty(node.nodeId)) return;
        if (pickedNodeIds.Contains(node.nodeId)) return;
        if (!string.IsNullOrEmpty(node.skillEffectId))
        {
            if (!activeSkillEffectIds.Contains(node.skillEffectId))
                activeSkillEffectIds.Add(node.skillEffectId);
        }

        pickedNodeIds.Add(node.nodeId);
        pickedNodes.Add(node);
        currentTier = Mathf.Max(currentTier, node.tier + 1);
    }
}
