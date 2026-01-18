using System.Collections.Generic;
using UnityEngine;

public class UnitCapacityManager : MonoBehaviour
{
    public static UnitCapacityManager Instance;

    // How many units of each definition are currently alive OR spawning
    private Dictionary<UnitDefinition, int> aliveCount = new();

    private Dictionary<UnitDefinition, int> extraCapacity = new();

    private void Awake()
    {
        Instance = this;
    }

    // Called once at battle start
    public void ResetForBattle()
    {
        aliveCount.Clear();
    }

    public int GetCapacity(UnitDefinition def)
    {
        int baseCap = def.baseCapacity;
        int bonus = extraCapacity.TryGetValue(def, out var v) ? v : 0;
        return baseCap + bonus;
    }

    public bool CanSpawn(UnitDefinition def)
    {
        int current = aliveCount.TryGetValue(def, out var v) ? v : 0;
        return current < GetCapacity(def);
    }

    // MUST be called exactly once per unit appearance (including pre-placed)
    public void RegisterSpawn(UnitDefinition def)
    {
        if (!aliveCount.ContainsKey(def))
            aliveCount[def] = 0;

        aliveCount[def]++;
    }

    // MUST be called on death
    public void RegisterDeath(UnitDefinition def)
    {
        if (!aliveCount.ContainsKey(def))
            return;

        aliveCount[def] = Mathf.Max(0, aliveCount[def] - 1);
    }

    // Future upgrade hook
    public void AddCapacity(UnitDefinition def, int amount)
    {
        if (!extraCapacity.ContainsKey(def))
            extraCapacity[def] = 0;

        extraCapacity[def] += amount;
    }
}
