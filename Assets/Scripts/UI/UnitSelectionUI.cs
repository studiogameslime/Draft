using System.Collections.Generic;
using UnityEngine;

public class UnitSelectionUI : MonoBehaviour
{
    [Header("Units pool")]
    public UnitDefinition[] allUnits;
    public int buttonsPerRoll = 3;

    [Header("UI")]
    public Transform buttonsParent;
    public UnitSpawnButton buttonPrefab;

    [HideInInspector]
    public BattleManager battleManager; // Injected externally

    private System.Random rng = new System.Random();

    // Create random unit buttons
    public void RollNewUnits()
    {
        // Clear previous buttons
        foreach (Transform child in buttonsParent)
            Destroy(child.gameObject);

        if (allUnits == null || allUnits.Length == 0)
            return;

        List<int> used = new List<int>();
        int countToSpawn = Mathf.Min(buttonsPerRoll, allUnits.Length);

        // Select random unique indices
        while (used.Count < countToSpawn)
        {
            int index = rng.Next(allUnits.Length);
            if (!used.Contains(index))
                used.Add(index);
        }

        // Instantiate buttons
        foreach (int i in used)
        {
            UnitDefinition def = allUnits[i];
            UnitSpawnButton btn = Instantiate(buttonPrefab, buttonsParent);
            btn.Init(def, this);     // Pass unit definition + UI reference
        }
    }

    // Legacy: only used for old selection UI
    public void OnUnitChosen(UnitDefinition def)
    {
        if (battleManager != null)
            battleManager.OnPlayerPickedUnit(def);
    }
}
