using System.Collections;
using UnityEngine;

public class SoulOrb : MonoBehaviour
{
    [Header("Canvas refs")]
    [SerializeField] private Canvas canvas;                 // ה-Canvas של ה-HUD
    [SerializeField] private RectTransform canvasRoot;      // בדרך כלל: (RectTransform)canvas.transform
    [SerializeField] private Transform selfRect;        // ה-RectTransform של האורב עצמו
    [SerializeField] private Camera uiCamera;               // אם Canvas = ScreenSpaceCamera/WorldSpace, לשים פה את הקאמרה. אם Overlay -> null

    [Header("Phase 1: Rise up")]
    public float riseHeight = 80f;            // ב-UI זה פיקסלים, לא מטרים
    public float riseDuration = 0.25f;
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Phase 2: Move towards pool")]
    public float moveToTargetDuration = 0.5f;
    public float arcHeight = 120f;           // פיקסלים
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Amount")]
    public int soulsAmount = 1;

    private RectTransform _targetUI;
    private bool _initialized;

    // startPos זה עדיין WORLD (מהדמות)
    public void Init(Vector3 startWorldPos, RectTransform targetUI, int amount = 1)
    {
        soulsAmount = Mathf.Max(1, amount);
        _targetUI = targetUI;

        if (canvas == null) canvas = GetComponentInParent<Canvas>();
        if (canvasRoot == null && canvas != null) canvasRoot = (RectTransform)canvas.transform;
        if (selfRect == null) selfRect = transform;

        // אם Overlay -> uiCamera = null. אם ScreenSpaceCamera/WorldSpace -> תן את canvas.worldCamera
        if (uiCamera == null && canvas != null && canvas.renderMode != RenderMode.ScreenSpaceOverlay)
            uiCamera = canvas.worldCamera;

        // להציב את האורב על נקודת התחלה ב-UI
        //selfRect.anchoredPosition = WorldToCanvasLocal(startWorldPos);

        _initialized = true;
        StartCoroutine(FlyToTargetRoutine(startWorldPos));
    }

    private IEnumerator FlyToTargetRoutine(Vector3 startWorldPos)
    {
        if (!_initialized || _targetUI == null || canvasRoot == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // ---------- Phase 1: Rise up (UI) ----------
        Vector2 riseStart = WorldToCanvasLocal(startWorldPos);
        Vector2 riseEnd = riseStart + Vector2.up * riseHeight;

        float t = 0f;
        float safeRiseDuration = Mathf.Max(0.01f, riseDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / safeRiseDuration;
            float k = riseCurve.Evaluate(Mathf.Clamp01(t));
            yield return null;
        }

        // ---------- Phase 2: Bezier arc to target (UI) ----------
        t = 0f;
        float safeMoveDuration = Mathf.Max(0.01f, moveToTargetDuration);

        // שים לב: היעד יכול לזוז (Layout וכו'), אז נחשב end כל פריים
        while (t < 1f)
        {
            t += Time.deltaTime / safeMoveDuration;
            float k = moveCurve.Evaluate(Mathf.Clamp01(t));

            Vector2 p0 = riseEnd;

            Vector2 end = GetTargetAnchoredPos(); // יעד ב-anchoredPosition ביחס ל-canvasRoot
            Vector2 mid = (p0 + end) * 0.5f + Vector2.up * arcHeight;

            Vector2 a = Vector2.Lerp(p0, mid, k);
            Vector2 b = Vector2.Lerp(mid, end, k);
            Vector2 pos = Vector2.Lerp(a, b, k);

            yield return null;
        }

        // ---------- Arrived ----------
        if (SoulsManager.instance != null)
            SoulsManager.instance.AddSouls(soulsAmount);

        Destroy(gameObject);
    }

    private Vector2 WorldToCanvasLocal(Vector3 worldPos)
    {
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, worldPos);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screen, uiCamera, out Vector2 local);
        return local;
    }

    private Vector2 GetTargetAnchoredPos()
    {
        // ממיר את מיקום ה-target (UI) ל-local של canvasRoot
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(uiCamera, _targetUI.position);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRoot, screen, uiCamera, out Vector2 local);
        return local;
    }
}
