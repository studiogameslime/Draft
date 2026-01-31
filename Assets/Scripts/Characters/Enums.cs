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
    King = 6
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
    Specials
}

public enum AdRewardType
{
    FreeChest
}

public enum CellBonusType
{
    None,
    HpPercent,
    AttackPercent
}

public enum RewardType
{
    Gold,
    Gems,
    Chest,
    Part,
    DiscoverNewUnit
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





