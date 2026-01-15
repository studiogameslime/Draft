using UnityEngine;

public class GateTrigger : MonoBehaviour
{
    private void OnTriggerEnter2D(Collider2D other)
    {
        var stats = other.GetComponent<CharacterStats>();
        if (stats == null) return;

        if (stats.team == Team.MyTeam)
        {
            stats.isUntargetable = false;
        }
    }
}
