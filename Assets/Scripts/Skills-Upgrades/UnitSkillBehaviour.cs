using UnityEngine;

public abstract class UnitSkillBehaviour : MonoBehaviour
{
    protected CharacterStats stats;

    public virtual void Init(CharacterStats s)
    {
        stats = s;
        OnInit();
    }

    protected virtual void OnInit() { }
}
