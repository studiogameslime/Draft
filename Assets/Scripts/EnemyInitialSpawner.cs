using UnityEngine;

public class EnemyInitialSpawner : MonoBehaviour
{
    public MonsterGrid enemyGrid;
    public GameObject[] enemyPrefabs;
    public int unitsToSpawn = 3;

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

        for (int i = 0; i < unitsToSpawn; i++)
        {
            GameObject prefab = enemyPrefabs[Random.Range(0, enemyPrefabs.Length)];

            // Spawn manually instead of AddMonster (AddMonster forces team = MyTeam)
            GameObject monster = Instantiate(prefab, enemyGrid.transform);

            var stats = monster.GetComponent<CharacterStats>();
            MonsterType type = stats.monsterType;

            // Init as Enemy
            stats.Init(Team.EnemyTeam, type);

            // Prevent grid from rearranging them later
            stats.lockedIn = true;

            // Disable AI until battle starts
            var ai = monster.GetComponent<EnemyAI>();
            if (ai != null) ai.enabled = false;
        }

        // Finally: arrange grid positions
        enemyGrid.ArrangeMonsters();
    }
}
