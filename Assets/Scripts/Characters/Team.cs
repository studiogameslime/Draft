
public enum MonsterType
{
    Melee,
    Ranged
}

public enum Team
    {
        MyTeam,
        EnemyTeam
    }

public interface IAttackStrategy
{
    void Attack(CharacterStats target);
}






