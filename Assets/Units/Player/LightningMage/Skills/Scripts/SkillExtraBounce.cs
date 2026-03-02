using UnityEngine;

public class SkillExtraBounce : UnitSkillBehaviour
{
    protected override void OnInit()
    {
        int extra = Mathf.Max(1, node.skillIntValue);

        var attack = GetComponent<LightningMageAttack>();
        if (attack != null)
            attack.maxBounces += extra;
    }
}
