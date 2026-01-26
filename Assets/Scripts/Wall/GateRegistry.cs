using System.Collections.Generic;
using UnityEngine;

public static class GateRegistry
{
    private static readonly List<GateController> gates = new List<GateController>();

    public static void Register(GateController gate)
    {
        if (gate == null) return;
        if (!gates.Contains(gate))
            gates.Add(gate);
    }

    public static void Unregister(GateController gate)
    {
        if (gate == null) return;
        gates.Remove(gate);
    }

    public static GateController GetClosestGate(Vector3 fromPosition)
    {
        GateController best = null;
        float bestDist = float.PositiveInfinity;

        for (int i = 0; i < gates.Count; i++)
        {
            var g = gates[i];
            if (g == null) continue;

            float d = g.DistanceToClosestEntry(fromPosition);
            if (d < bestDist)
            {
                bestDist = d;
                best = g;
            }
        }

        return best;
    }
}
