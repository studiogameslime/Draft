using System;
using TMPro;
using Unity.VisualScripting;
using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Controls the Missions screen content (daily / weekly)
/// and delegates the open animation to PopupAnimator.
/// </summary>
public class MissionsScreen : MonoBehaviour
{
    [Header("Prefabs")]
    [SerializeField] private MissionContainer missionContainerPrefab;

    [Header("Content Parents")]
    [SerializeField] private Transform dailyContent;
    [SerializeField] private Transform weeklyContent;

    [SerializeField] private Button dailyTab;
    [SerializeField] private Button weeklyTab;
    [SerializeField] private Sprite selectedTabSprite;
    [SerializeField] private Sprite unselectedTabSprite;


    [SerializeField] private TMP_Text dailyResetText;
    [SerializeField] private TMP_Text weeklyResetText;

    [Header("Popup Animation")]
    [SerializeField] private PopupAnimator popupAnimator;
    [SerializeField] private GameObject root;

    [SerializeField] private MissionsCompletionBar completionBar;


    private void Awake()
    {
        root.SetActive(true); // init
    }

    private void Start()
    {
        root.SetActive(false); // closed by default
    }

    private void Update()
    {
        if (MissionsManager.Instance == null)
            return;

        UpdateDaily();
        UpdateWeekly();
    }

    // =========================================================
    // Reset timers preview
    // =========================================================
    private void UpdateDaily()
    {
        DateTime nextUtc = DailyResetUtil.GetNextDailyResetUtc(DateTime.UtcNow);
        TimeSpan left = nextUtc - DateTime.UtcNow;

        dailyResetText.text = FormatTime(left);
    }



    private void UpdateWeekly()
    {
        DateTime nextUtc = DailyResetUtil.GetNextWeeklyResetUtc(DateTime.UtcNow);
        TimeSpan left = nextUtc - DateTime.UtcNow;

        weeklyResetText.text = FormatTime(left);
    }


    private string FormatTime(TimeSpan t)
    {
        if (t.TotalSeconds < 0)
            return "00:00:00";

        if (t.TotalDays >= 1)
            return $"{t.Days}d {t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";

        return $"{t.Hours:D2}:{t.Minutes:D2}:{t.Seconds:D2}";
    }
    // =========================================================
    // TAB BUTTONS
    // =========================================================

    /// <summary>
    /// Show daily missions tab.
    /// </summary>
    public void ShowDaily()
    {
        dailyContent.gameObject.SetActive(true);
        weeklyContent.gameObject.SetActive(false);
        dailyResetText.gameObject.SetActive(true);
        weeklyResetText.gameObject.SetActive(false);

        SelectDailyTab();
        completionBar.ShowDaily();
        RefreshDailyMissions();
    }

    /// <summary>
    /// Show weekly missions tab.
    /// </summary>
    public void ShowWeekly()
    {
        dailyContent.gameObject.SetActive(false);
        weeklyContent.gameObject.SetActive(true);
        dailyResetText.gameObject.SetActive(false);
        weeklyResetText.gameObject.SetActive(true);

        SelectWeeklyTab();
        completionBar.ShowWeekly();
        RefreshWeeklyMissions();
    }

    // =========================================================
    // REFRESH
    // =========================================================

    private void RefreshDailyMissions()
    {
        Clear(dailyContent);

        foreach (var mission in MissionsManager.Instance.ActiveDailyMissions)
        {
            var item = Instantiate(missionContainerPrefab, dailyContent);
            item.Setup(mission);
        }
    }

    private void RefreshWeeklyMissions()
    {
        Clear(weeklyContent);

        foreach (var mission in MissionsManager.Instance.ActiveWeeklyMissions)
        {
            var item = Instantiate(missionContainerPrefab, weeklyContent);
            item.Setup(mission);
        }
    }

    private void Clear(Transform parent)
    {
        for (int i = parent.childCount - 1; i >= 0; i--)
        {
            Destroy(parent.GetChild(i).gameObject);
        }
    }

    // =========================================================
    // OPEN / CLOSE
    // =========================================================

    /// <summary>
    /// Opens the missions popup from a specific button rect
    /// (used for "open from button" animation).
    /// This should be called directly from the button OnClick.
    /// </summary>
    public void OpenFromButton(RectTransform buttonRect)
    {
        ShowDaily();

        if (popupAnimator == null)
        {
            Debug.LogWarning("MissionsScreen: PopupAnimator is not assigned.");
            return;
        }
        root.SetActive(true);

        popupAnimator.OpenFromRect(buttonRect);
        Debug.Log(DateTime.UtcNow);
        Debug.Log($"GetCurrentDailyResetUtc - {DailyResetUtil.GetCurrentDailyResetUtc(DateTime.UtcNow)}");
        Debug.Log($"lastDailyLoginUtcTicks - {GameData.Instance.Save.lastDailyLoginUtcTicks}");
    }

    /// <summary>
    /// Close the missions popup.
    /// Usually wired to overlay / close button.
    /// </summary>
    public void Close()
    {
        if (popupAnimator == null)
            return;

        //root.SetActive(false);
        popupAnimator.Close();
    }

    private void SelectDailyTab()
    {
        dailyTab.image.sprite = selectedTabSprite;
        weeklyTab.image.sprite = unselectedTabSprite;

    }
    private void SelectWeeklyTab()
    {
        weeklyTab.image.sprite = selectedTabSprite;
        dailyTab.image.sprite = unselectedTabSprite;
    }
}