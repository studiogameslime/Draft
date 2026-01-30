using UnityEngine;

public abstract class UnitSkillBehaviour : MonoBehaviour
{
    protected CharacterStats stats;
    protected UnitUpgradeNodeDefinition node;

    public void Init(CharacterStats s, UnitUpgradeNodeDefinition n)
    {
        stats = s;
        node = n;
        OnInit();
    }

    protected virtual void OnInit() { }
}
