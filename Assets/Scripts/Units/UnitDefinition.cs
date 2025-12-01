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
    // public int cost;           // optional: price, mana cost, etc.
}
