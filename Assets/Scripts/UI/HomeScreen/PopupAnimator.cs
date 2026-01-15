using UnityEngine;
using System.Collections;

public class PopupAnimator : MonoBehaviour
{
    [Header("References")]
    [SerializeField] private GameObject root;
    [SerializeField] private RectTransform windowRect;
    [SerializeField] private CanvasGroup overlayCanvasGroup;
    [SerializeField] private Canvas rootCanvas;

    [Header("Open Animation")]
    [SerializeField] private float openDuration = 0.25f;
    [SerializeField] private Vector3 startScale = new Vector3(0.15f, 0.15f, 1f);

    [Header("Close Animation")]
    [SerializeField] private float closeDuration = 0.2f;

    private Vector2 _finalAnchoredPos;
    private Vector3 _finalScale = Vector3.one;
    private RectTransform _lastFromRect;
    private Coroutine _routine;

    private void Awake()
    {
        _finalAnchoredPos = windowRect.anchoredPosition;

        if (root != null)
            root.SetActive(false);
    }

    // =========================
    // OPEN
    // =========================
    public void OpenFromRect(RectTransform fromRect, bool hideButton = false)
    {
        if (fromRect == null)
        {
            Debug.LogWarning("PopupAnimator.OpenFromRect: fromRect is null");
            return;
        }

        _lastFromRect = fromRect;
        if (hideButton) _lastFromRect.gameObject.SetActive(false);//Hide the button
        root.SetActive(true);

        if (overlayCanvasGroup != null)
        {
            overlayCanvasGroup.alpha = 0f;
            overlayCanvasGroup.blocksRaycasts = true;
        }

        windowRect.anchoredPosition = GetAnchoredPositionInCanvas(fromRect);
        windowRect.localScale = startScale;

        RestartRoutine(OpenRoutine());
    }

    // =========================
    // CLOSE
    // =========================
    public void Close()
    {
        if (!root.activeSelf)
            return;

        if (_lastFromRect == null)
        {
            root.SetActive(false);
            return;
        }

        RestartRoutine(CloseRoutine());
    }

    // =========================
    // ROUTINES
    // =========================
    private IEnumerator OpenRoutine()
    {
        float t = 0f;
        Vector2 startPos = windowRect.anchoredPosition;
        Vector3 startScaleLocal = windowRect.localScale;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, openDuration);
            float eased = EaseOutCubic(t);

            windowRect.anchoredPosition =
                Vector2.Lerp(startPos, _finalAnchoredPos, eased);
            windowRect.localScale =
                Vector3.Lerp(startScaleLocal, _finalScale, eased);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = Mathf.Lerp(0f, 1f, eased);

            yield return null;
        }

        windowRect.anchoredPosition = _finalAnchoredPos;
        windowRect.localScale = _finalScale;
    }

    private IEnumerator CloseRoutine()
    {
        float t = 0f;
        Vector2 startPos = windowRect.anchoredPosition;
        Vector2 endPos = GetAnchoredPositionInCanvas(_lastFromRect);
        Vector3 startScaleLocal = windowRect.localScale;

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, closeDuration);
            float eased = EaseOutCubic(t);

            windowRect.anchoredPosition =
                Vector2.Lerp(startPos, endPos, eased);
            windowRect.localScale =
                Vector3.Lerp(startScaleLocal, startScale, eased);

            if (overlayCanvasGroup != null)
                overlayCanvasGroup.alpha = Mathf.Lerp(1f, 0f, eased);

            yield return null;
        }
        _lastFromRect.gameObject.SetActive(true);//Show the button

        root.SetActive(false);
    }

    // =========================
    // HELPERS
    // =========================
    private void RestartRoutine(IEnumerator routine)
    {
        if (_routine != null)
            StopCoroutine(_routine);

        _routine = StartCoroutine(routine);
    }

    private Vector2 GetAnchoredPositionInCanvas(RectTransform fromRect)
    {
        Camera cam = rootCanvas.renderMode == RenderMode.ScreenSpaceOverlay
            ? null
            : rootCanvas.worldCamera;

        Vector3 worldCenter = fromRect.TransformPoint(fromRect.rect.center);
        Vector2 screenPoint =
            RectTransformUtility.WorldToScreenPoint(cam, worldCenter);

        RectTransform parentRect = windowRect.parent as RectTransform;

        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            parentRect, screenPoint, cam, out Vector2 localPoint);

        return localPoint;
    }

    private float EaseOutCubic(float x)
    {
        x = Mathf.Clamp01(x);
        float p = 1f - x;
        return 1f - (p * p * p);
    }
}
