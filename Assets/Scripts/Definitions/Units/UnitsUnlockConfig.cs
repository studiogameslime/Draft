using UnityEngine;

[CreateAssetMenu(menuName = "Units/Unlock Config")]
public class UnitsUnlockConfig : ScriptableObject
{
    public UnitDefinition[] allUnits;    
    public UnitDefinition[] startingUnits; 
}
