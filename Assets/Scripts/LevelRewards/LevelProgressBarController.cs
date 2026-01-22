using UnityEngine;
using UnityEngine.UI;

public class LevelProgressBarController : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private LevelRewardWindowController window;
    [SerializeField] private RectTransform content;     // ScrollView/Viewport/Content
    [SerializeField] private Image fillImage;           // ProgressBarOverlay/Fill (Image Filled Vertical Bottom)
    [SerializeField] private RectTransform xpMarker;    // optional (can be null)

    [Header("Tuning")]
    [SerializeField] private float markerYOffset = 0f;


    private System.Action<int> _xpHandler;
    private System.Action<int> _levelHandler;

    private void OnEnable()
    {
        var xp = PlayerXPManager.Instance;
        if (xp != null)
        {
            _xpHandler = _ => Refresh();
            _levelHandler = _ => Refresh();
            xp.OnXPChanged += _xpHandler;
            xp.OnLevelChanged += _levelHandler;
        }

        if (window != null)
            window.OnRowsBuilt += Refresh;

        StartCoroutine(RefreshNextFrame());
    }

    private void OnDisable()
    {
        var xp = PlayerXPManager.Instance;
        if (xp != null)
        {
            if (_xpHandler != null) xp.OnXPChanged -= _xpHandler;
            if (_levelHandler != null) xp.OnLevelChanged -= _levelHandler;
        }

        if (window != null)
            window.OnRowsBuilt -= Refresh;
    }


    private System.Collections.IEnumerator RefreshNextFrame()
    {
        yield return null;
        Refresh();
    }

    public void Refresh()
    {
        var xp = PlayerXPManager.Instance;
        if (xp == null || window == null || content == null || fillImage == null)
            return;

        int currentLevel = Mathf.Max(1, xp.currentLevel);
        float xp01 = Mathf.Clamp01((float)xp.currentXP / xp.GetXPForNextLevel());

        RectTransform anchorA = window.GetLevelAnchor(currentLevel);
        RectTransform anchorB = window.GetLevelAnchor(currentLevel + 1);


        if (anchorA == null) return;

        float yA = GetAnchorY_InContent(anchorA);
        float yB = (anchorB != null) ? GetAnchorY_InContent(anchorB) : yA;


        float yTarget = Mathf.Lerp(yA, yB, xp01);

        Debug.Log($"Level={currentLevel} xp01={xp01} yA={yA} yTarget={yTarget}");
        Debug.Log($"AnchorA world={anchorA.position} contentLocal={content.InverseTransformPoint(anchorA.position)}");


        // 1) Fill amount based on position between first row and last row
        RectTransform row1 = window.GetLevelAnchor(1);
        RectTransform rowLast = window.GetLevelAnchor(windowMaxLevelGuess());

        if (row1 != null && rowLast != null)
        {
            float yBottom = GetRowCenterY_InContent(row1);
            float yTop = GetRowCenterY_InContent(rowLast);
            float t = Mathf.InverseLerp(yBottom, yTop, yTarget);
            fillImage.fillAmount = Mathf.Clamp01(t);
        }

        // 2) Optional marker position
        if (xpMarker != null)
        {
            Vector3 world = content.TransformPoint(new Vector3(0f, yTarget, 0f));
            Vector3 local = xpMarker.parent.InverseTransformPoint(world);
            var p = xpMarker.anchoredPosition;
            p.y = local.y + markerYOffset;
            xpMarker.anchoredPosition = p;
        }
    }

    private float GetRowCenterY_InContent(RectTransform row)
    {
        Vector3 worldCenter = row.TransformPoint(row.rect.center);
        Vector3 localInContent = content.InverseTransformPoint(worldCenter);
        return localInContent.y;
    }

    private int windowMaxLevelGuess()
    {

        return 30;
    }
    private float GetAnchorY_InContent(RectTransform anchor)
    {
        Vector3 worldCenter = anchor.TransformPoint(anchor.rect.center);
        Vector3 local = content.InverseTransformPoint(worldCenter);
        return local.y;
    }



}
