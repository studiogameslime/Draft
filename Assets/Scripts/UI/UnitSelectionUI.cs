using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionUI : MonoBehaviour
{
    [Header("Units pool")]
    public UnitDefinition[] allUnits;   // assign all 20+ unit definitions here in the Inspector
    public int buttonsPerRoll = 3;

    [Header("UI")]
    public Transform buttonsParent;     // empty GameObject with Horizontal/Vertical Layout Group
    public UnitSpawnButton buttonPrefab;     // the UnitButton prefab

    [Header("Spawn")]
    public MonsterGrid leftGrid;        // your existing grid for player units

    private System.Random rng = new System.Random();

    private void Start()
    {
        RollNewUnits();
    }

    // Creates a new random set of buttons
    public void RollNewUnits()
    {
        // Clear previous buttons
        foreach (Transform child in buttonsParent)
        {
            Destroy(child.gameObject);
        }

        if (allUnits == null || allUnits.Length == 0)
            return;

        // Pick unique random indices
        List<int> used = new List<int>();
        int countToSpawn = Mathf.Min(buttonsPerRoll, allUnits.Length);

        while (used.Count < countToSpawn)
        {
            int index = rng.Next(allUnits.Length);
            if (!used.Contains(index))
                used.Add(index);
        }

        // Create buttons
        foreach (int i in used)
        {
            UnitDefinition def = allUnits[i];
            UnitSpawnButton btn = Instantiate(buttonPrefab, buttonsParent);
            btn.Init(def, this);
        }
    }

    // Called by UnitButton when player clicks a unit
    public void OnUnitChosen(UnitDefinition def)
    {
        if (def == null || leftGrid == null || def.prefab == null)
            return;

        // Spawn into left grid (player side)
        leftGrid.AddMonster(def.prefab, def.monsterType);

        // Optional: immediately reroll new 3 units:
        // RollNewUnits();
    }
}
