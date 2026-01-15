using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MissionsCompletionBar : MonoBehaviour
{
    [SerializeField] private TMP_Text progressText;
    [SerializeField] private Image progressFill;

    private bool showingDaily = true;


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


    //private void OnEnable()
    //{
    //    if (MissionsManager.Instance == null)
    //        return;

    //    MissionsManager.Instance.OnMissionsStateChanged += Refresh;
    //    Refresh();
    //}


    //private void OnDisable()
    //{
    //    if (MissionsManager.Instance != null)
    //        MissionsManager.Instance.OnMissionsStateChanged -= Refresh;
    //}

    public void ShowDaily()
    {
        showingDaily = true;
        Refresh();
    }

    public void ShowWeekly()
    {
        showingDaily = false;
        Refresh();
    }

    private void Refresh()
    {
        if (MissionsManager.Instance == null)
            return;

        int claimed;
        int total;

        if (showingDaily)
        {
            claimed = MissionsManager.Instance.GetClaimedDailyCount();
            total = MissionsManager.Instance.GetTotalDailyCount();
        }
        else
        {
            claimed = MissionsManager.Instance.GetClaimedWeeklyCount();
            total = MissionsManager.Instance.GetTotalWeeklyCount();
        }

        progressText.text = $"{claimed} / {total}";
        progressFill.fillAmount = total > 0 ? (float)claimed / total : 0f;
    }
}
