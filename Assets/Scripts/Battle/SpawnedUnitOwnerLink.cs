using UnityEngine;

/// <summary>
/// Lives on spawned FIGHTER units.
/// Holds a reference back to the UnitSpawner that created it.
/// </summary>
public class SpawnedUnitOwnerLink : MonoBehaviour
{
    [HideInInspector] public UnitSpawner owner;

    public void NotifyOwnerDied()
    {
        if (owner != null)
            owner.NotifyUnitDied();
    }
}
