using UnityEngine;

[RequireComponent(typeof(WallHealth))]
public class WallCombatTarget : MonoBehaviour, ICombatTarget
{
    private WallHealth wall;

    public Transform TargetTransform => transform;
    public bool IsAlive => wall != null && !wall.destroyed && wall.currentHealth > 0;
    public bool IsUntargetable => false;

    private void Awake() => wall = GetComponent<WallHealth>();

    public void TakeDamage(int amount, CharacterStats attacker)
    {
        wall?.TakeDamage(amount);
    }
}
