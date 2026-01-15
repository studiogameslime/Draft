using System.Collections;
using UnityEngine;

public class SoulOrb : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private Camera worldCamera;          // לרוב Camera.main
    [SerializeField] private Canvas uiCanvas;             // ה-Canvas שבו נמצא היעד
    [SerializeField] private Camera uiCamera;             // Overlay = null, ScreenSpaceCamera/WorldSpace = canvas.worldCamera
    [SerializeField] private float screenDepthOffset = 0f; // אופציונלי: להזיז קצת קדימה/אחורה

    [Header("Phase 1: Rise up (in SCREEN pixels)")]
    public float riseHeight = 80f;
    public float riseDuration = 0.25f;
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Phase 2: Move towards UI target (in SCREEN pixels)")]
    public float moveToTargetDuration = 0.5f;
    public float arcHeight = 120f;
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Amount")]
    public int soulsAmount = 1;

    private RectTransform _targetUI;
    private Vector3 _startWorldPos;
    private float _depth; // עומק מסך קבוע (z ב-ScreenPoint)

    public void Init(Vector3 startPosWorld, RectTransform targetUI, int amount = 1)
    {
        _startWorldPos = startPosWorld;
        _targetUI = targetUI;
        soulsAmount = Mathf.Max(1, amount);

        if (worldCamera == null) worldCamera = Camera.main;
        if (uiCanvas != null && uiCanvas.renderMode != RenderMode.ScreenSpaceOverlay && uiCamera == null)
            uiCamera = uiCanvas.worldCamera;

        // קובעים עומק קבוע לפי נקודת ההתחלה (כדי שהאורב "יישאר" באותו מרחק מהמצלמה)
        var startScreen3 = worldCamera.WorldToScreenPoint(_startWorldPos);
        _depth = startScreen3.z + screenDepthOffset;

        // מציבים את האורב על נקודת ההתחלה
        transform.position = _startWorldPos;

        StartCoroutine(FlyToTarget());
    }

    private IEnumerator FlyToTarget()
    {
        if (worldCamera == null || _targetUI == null)
        {
            Destroy(gameObject);
            yield break;
        }

        // --- Screen start/end ---
        Vector2 startScreen = (Vector2)worldCamera.WorldToScreenPoint(_startWorldPos);
        Vector2 endScreen = GetTargetScreenPos(); // יעד ב-Screen

        // ---------- Phase 1: Rise up ----------
        Vector2 riseStart = startScreen;
        Vector2 riseEnd = startScreen + Vector2.up * riseHeight;

        float t = 0f;
        float safeRise = Mathf.Max(0.01f, riseDuration);
        while (t < 1f)
        {
            t += Time.deltaTime / safeRise;
            float k = riseCurve.Evaluate(Mathf.Clamp01(t));

            Vector2 screenPos = Vector2.Lerp(riseStart, riseEnd, k);
            SetWorldFromScreen(screenPos);

            yield return null;
        }

        // ---------- Phase 2: Bezier arc to UI target ----------
        t = 0f;
        float safeMove = Mathf.Max(0.01f, moveToTargetDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / safeMove;
            float k = moveCurve.Evaluate(Mathf.Clamp01(t));

            // יעד יכול לזוז (Layout/UI), אז מחשבים אותו כל פריים
            endScreen = GetTargetScreenPos();

            Vector2 p0 = riseEnd;
            Vector2 p2 = endScreen;
            Vector2 p1 = (p0 + p2) * 0.5f + Vector2.up * arcHeight;

            Vector2 a = Vector2.Lerp(p0, p1, k);
            Vector2 b = Vector2.Lerp(p1, p2, k);
            Vector2 screenPos = Vector2.Lerp(a, b, k);

            SetWorldFromScreen(screenPos);

            yield return null;
        }

        // ---------- Arrived ----------
        if (SoulsManager.instance != null)
            SoulsManager.instance.AddSouls(soulsAmount);

        Destroy(gameObject);
    }

    private Vector2 GetTargetScreenPos()
    {
        // עובד לכל סוגי Canvas: Overlay/Camera/WorldSpace
        return RectTransformUtility.WorldToScreenPoint(uiCamera, _targetUI.position);
    }

    private void SetWorldFromScreen(Vector2 screenPos)
    {
        // ScreenToWorldPoint צריך z=depth (מרחק מהמצלמה)
        var sp = new Vector3(screenPos.x, screenPos.y, Mathf.Max(0.01f, _depth));
        Vector3 world = worldCamera.ScreenToWorldPoint(sp);
        transform.position = world;
    }
}
