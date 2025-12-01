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

    [HideInInspector] public BattleManager battleManager;

    private System.Random rng = new System.Random();

    public void RollNewUnits()
    {
        foreach (Transform child in buttonsParent)
            Destroy(child.gameObject);

        if (allUnits == null || allUnits.Length == 0) return;

        List<int> used = new List<int>();
        int countToSpawn = Mathf.Min(buttonsPerRoll, allUnits.Length);

        while (used.Count < countToSpawn)
        {
            int index = rng.Next(allUnits.Length);
            if (!used.Contains(index))
                used.Add(index);
        }

        foreach (int i in used)
        {
            UnitDefinition def = allUnits[i];
            UnitSpawnButton btn = Instantiate(buttonPrefab, buttonsParent);
            btn.Init(def, this);
        }
    }

    public void OnUnitChosen(UnitDefinition def)
    {
        if (battleManager != null)
        {
            battleManager.OnPlayerPickedUnit(def);
        }
    }
}
