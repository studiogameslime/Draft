using System.Linq;

public static class BattleUpgradeLogic
{
    public static UnitUpgradeNodeDefinition FindNode(UnitDefinition unit, int tier, UpgradeLane lane)
    {
        if (unit == null || unit.nodes == null) return null;

        return unit.nodes.FirstOrDefault(n =>
            n != null &&
            (int)n.tier == tier &&
            n.lane == lane
        );
    }
}
