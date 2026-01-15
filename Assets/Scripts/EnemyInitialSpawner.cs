using UnityEngine;

public class EnemyInitialSpawner : MonoBehaviour
{
    public MonsterGrid enemyGrid;
    public UnitDefinition[] enemyUnits;  // instead of GameObject[]
    public int unitsToSpawn = 3;
    public int enemyLevel = 1;

    private void Start()
    {
        SpawnEnemyArmy();
    }

    private void SpawnEnemyArmy()
    {
        if (enemyGrid == null)
        {
            Debug.LogError("EnemyInitialSpawner: enemyGrid is NULL! Assign EnemyTeam object.");
            return;
        }

        if (enemyUnits == null || enemyUnits.Length == 0)
        {
            Debug.LogError("EnemyInitialSpawner: enemyUnits is empty! Assign UnitDefinitions.");
            return;
        }

        for (int i = 0; i < unitsToSpawn; i++)
        {
            UnitDefinition def = enemyUnits[Random.Range(0, enemyUnits.Length)];

            // Enemy team, use UnitDefinition + level
            enemyGrid.AddMonster(def, Team.EnemyTeam, enemyLevel);
        }

        // Now MonsterGrid can position them correctly (horizontal)
        enemyGrid.ArrangeMonsters();
    }
}
