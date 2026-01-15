using UnityEngine;

public class MissionsButtonNotifier : MonoBehaviour
{
    [SerializeField] private GameObject exclamationIcon;

    private void Start()
    {
        if (MissionsManager.Instance != null)
        {
            MissionsManager.Instance.OnMissionsStateChanged += Refresh;
            Refresh();
        }
    }

    private void OnDestroy()
    {
        if (MissionsManager.Instance != null)
            MissionsManager.Instance.OnMissionsStateChanged -= Refresh;
    }

    private void Refresh()
    {
        if (exclamationIcon == null || MissionsManager.Instance == null)
            return;

        bool show = MissionsManager.Instance.HasUnclaimedCompletedMissions();
        exclamationIcon.SetActive(show);
    }
}
