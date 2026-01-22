using System.Collections.Generic;
using UnityEngine;

public class UnitSpawnerBattleUpgradeState : MonoBehaviour
{
    public int currentTier = 1;
    public HashSet<string> pickedNodeIds = new HashSet<string>();

    public void Pick(string nodeId)
    {
        if (string.IsNullOrEmpty(nodeId)) return;
        if (pickedNodeIds.Contains(nodeId)) return;

        pickedNodeIds.Add(nodeId);
        currentTier += 1;
    }
}
