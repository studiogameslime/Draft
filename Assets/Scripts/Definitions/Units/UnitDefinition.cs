using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unit Definition")]
public class UnitDefinition : ScriptableObject
{
    [Header("Basic info")]
    public string id;
    public string displayName;

    [Header("Visuals")]
    public Sprite icon;          // icon for the UI button
    public GameObject prefab;    // prefab to spawn in the grid
    public float iconScale = 1f;


    [Header("Gameplay")]
    public MonsterType monsterType; // Melee / Ranged etc.
    public int spawnCount = 1; // default 1

    [Header("Souls")]
    public int soulCost;
    [Range(0f, 1f)]
    public float soulDropChance = 0.25f; // 25% לדוגמה – תעדכן לכל יחידה באינספקטור
    // public int cost;           // optional: price, mana cost, etc.

    [Header("Stats")]
    public int maxHealth = 100;
    public int damage;
    public float moveSpeed = 2f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
}
