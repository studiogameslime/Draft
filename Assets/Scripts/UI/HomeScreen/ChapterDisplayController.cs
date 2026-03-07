using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class ChapterDisplayController : MonoBehaviour
{
    [Header("UI References")]
    [SerializeField] private Image chapterImage;
    [SerializeField] private TMP_Text chapterText;

    private void Start()
    {
        UpdateDisplay();
    }

    public void UpdateDisplay()
    {
        string stageId = "1-1";
        if (GameData.Instance != null && GameData.Instance.Save != null &&
            !string.IsNullOrEmpty(GameData.Instance.Save.currentStageId))
        {
            stageId = GameData.Instance.Save.currentStageId;
        }

        if (LevelsDatabase.Instance == null) return;

        var def = LevelsDatabase.Instance.GetOrNull(stageId);
        if (def == null) return;

        if (chapterImage != null && def.previewImage != null)
        {
            chapterImage.sprite = def.previewImage;
            chapterImage.preserveAspect = true;
        }

        if (chapterText != null && !string.IsNullOrEmpty(def.displayName))
            chapterText.text = def.displayName;
    }
}
