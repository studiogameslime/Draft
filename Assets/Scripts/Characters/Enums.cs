using UnityEngine;

public enum MonsterType
{
    Melee,
    Ranged,
    Supllier
}

public enum UnitClass
{
    None = 0,
    Melee = 1,
    Ranged = 3,
    Mage = 4,
    Support = 5,
    Wall = 6,
}


public enum Team
{
    MyTeam,
    EnemyTeam
}

public interface IAttackStrategy
{
    void Attack(ICombatTarget target);

}

public interface ICombatTarget
{
    Transform TargetTransform { get; }
    bool IsAlive { get; }
    bool IsUntargetable { get; }
    void TakeDamage(int amount, CharacterStats attacker);
}


public enum ChestRarity
{
    None,
    Common,
    Rare,
    Epic,
    Legendary
}


[System.Serializable]
public struct IntRange
{
    public int min;
    public int max;

    public int GetRandom()
    {
        if (max < min)
            max = min;
        return Random.Range(min, max + 1);
    }
}

public enum StoreCategory
{
    BuyGoldWithGems,
    BuyChestsWithGold,
    BuyPartWithGold,
    Specials,
    BuyScrollsWithGold
}

public enum AdRewardType
{
    FreeChest,
    FreeMasteryDraw,
    DoubleGold
}

public enum CellBonusType
{
    None,
    HpPercent,
    AttackPercent,
    SpawnTimePercent
}

public enum RewardType
{
    Gold,
    Gems,
    Chest,
    Part,
    DiscoverNewUnit,
    Scrolls,
    FeatureUnlock
}

public enum GameFeature
{
    Mastery = 0,
    Upgrades = 1
}

public enum RewardLane
{
    Right = 0, // Main reward (as in your UI right side)
    Left = 1   // Special reward (as in your UI left side)
}
public interface IOnHitEffect
{
    void OnHit(CharacterStats attacker, CharacterStats target);
}

public interface IOnProjectileSpawned
{
    void OnProjectileSpawned(Projectile projectile);
}

public interface IProjectileOnHit
{
    bool TryHandleAfterHit(Projectile p, CharacterStats attacker, CharacterStats hitTarget);
}

public enum FloatingNumberType
{
    Normal,
    Crit,
    Heal,
    Poison,
    Burn
}

public interface IMeteorSkill
{
    void OnMeteorSpawned(MeteorProjectile meteor);
}

public enum RangeLabel
{
    Melee,
    Short,
    Long
}
public enum UnitSpeedCategory
{
    Slow,
    Normal,
    Fast,
}




