using UnityEngine;

[CreateAssetMenu(menuName = "Game/Enemy Database")]
public class EnemyDatabase : ScriptableObject
{
    public UnitDefinition[] allEnemies;
}
