using System.Collections.Generic;
using UnityEngine;

[CreateAssetMenu(menuName = "Game/Units/Upgrade Node", fileName = "UpgradeNode_")]

public abstract class UnitSkillEffectDefinition : ScriptableObject
{
    public abstract UnitSkillBehaviour AddTo(GameObject host, CharacterStats stats);
}
