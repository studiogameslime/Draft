using UnityEngine;

public class SkillChainRange : UnitSkillBehaviour
{
    protected override void OnInit()
    {
        float extra = Mathf.Max(0.1f, node.skillRadiusValue);

        var attack = GetComponent<LightningMageAttack>();
        if (attack != null)
            attack.chainRange += extra;
    }
}
