using System.Collections.Generic;
using UnityEngine;

public class UnitSpawnerBattleUpgradeState : MonoBehaviour
{
    public int currentTier = 1;
    public HashSet<string> pickedNodeIds = new HashSet<string>();
    public List<UnitUpgradeNodeDefinition> pickedNodes = new List<UnitUpgradeNodeDefinition>();

    public void Pick(UnitUpgradeNodeDefinition node)
    {
        if (node == null) return;
        if (string.IsNullOrEmpty(node.nodeId)) return;
        if (pickedNodeIds.Contains(node.nodeId)) return;

        pickedNodeIds.Add(node.nodeId);
        pickedNodes.Add(node);
        currentTier = Mathf.Max(currentTier, node.tier + 1);
    }
}
