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

            GameObject monster = Instantiate(prefab, enemyGrid.transform);

            var stats = monster.GetComponent<CharacterStats>();

            MonsterType type = prefab.GetComponent<CharacterStats>().monsterType;
            stats.Init(Team.EnemyTeam, type);
            stats.monsterType = type;

            // DO NOT lock here!
            // stats.lockedIn = true;

            // Disable AI until battle starts
            var ai = monster.GetComponent<EnemyAI>();
            if (ai != null) ai.enabled = false;
        }

        // Now MonsterGrid can position them correctly (horizontal)
        enemyGrid.ArrangeMonsters();
    }
}
