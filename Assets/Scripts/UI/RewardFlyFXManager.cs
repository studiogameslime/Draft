using System;
using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public enum RewardType
{
    Gold,
    Gems,
    Part,
}

/// <summary>
/// Spawns UI icons that fly from a source RectTransform to a fixed HUD target.
/// Home-scene singleton (no DontDestroyOnLoad).
/// </summary>
public class RewardFlyFXManager : MonoBehaviour
{
    public static RewardFlyFXManager Instance { get; private set; }

    [Header("Canvas")]
    [SerializeField] private Canvas uiCanvas;
    [SerializeField] private RectTransform fxLayer;

    [Header("Icon Prefabs (UI Image)")]
    [SerializeField] private Image goldIconPrefab;
    [SerializeField] private Image gemsIconPrefab;
    [SerializeField] private Image partIconPrefab;

    [Header("Fixed Targets (HUD)")]
    [SerializeField] private RectTransform goldTarget;
    [SerializeField] private RectTransform gemsTarget;
    [SerializeField] private RectTransform unitsPartsTarget;

    [Header("Motion")]
    [SerializeField] private float flyDuration = 0.55f;
    [SerializeField] private AnimationCurve ease = AnimationCurve.EaseInOut(0, 0, 1, 1);
    [SerializeField] private float scatterRadius = 40f;
    [SerializeField] private float arcHeight = 120f;

    [Header("Spawn")]
    [SerializeField] private float spawnInterval = 0.03f;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void OnDestroy()
    {
        if (Instance == this)
            Instance = null;
    }

    /// <summary>
    /// Spawns multiple icons (prefabs) and flies them to the HUD target.
    /// </summary>
    public void Play(RewardType type, RectTransform fromUI, int iconCount, Action<float> onProgress, Action onComplete = null)
    {
        if (!CanPlay(fromUI, type))
        {
            onComplete?.Invoke();
            return;
        }

        iconCount = Mathf.Clamp(iconCount, 1, 20);

        RectTransform target = GetTarget(type);
        Vector2 from = WorldToCanvasPoint(fromUI.position);
        Vector2 to = WorldToCanvasPoint(target.position);

        StartCoroutine(SpawnBurstAndWait(type, from, to, iconCount, onProgress, onComplete));
    }

    /// <summary>
    /// Detaches an existing UI RectTransform (the actual reward visual) and flies it to the HUD target.
    /// A placeholder is inserted to keep the grid/layout stable.
    /// The flying object is destroyed at the end.
    /// </summary>
    public void FlyExistingToTarget(RewardType type, RectTransform existing, Action onComplete = null)
    {
        if (!CanFlyExisting(existing, type))
        {
            onComplete?.Invoke();
            return;
        }

        StartCoroutine(FlyExistingRoutine(type, existing, onComplete));
    }

    /// <summary>
    /// Converts an amount (gold/gems) into an icon count automatically.
    /// </summary>
    public int GetIconCountForAmount(int amount)
    {
        if (amount <= 0) return 0;

        float a = Mathf.Clamp(amount, 1, 5000);
        float t = Mathf.Log10(a + 1f);
        int count = Mathf.RoundToInt(3f + t * 4.5f);
        return Mathf.Clamp(count, 3, 18);
    }

    private bool CanPlay(RectTransform fromUI, RewardType type)
    {
        if (uiCanvas == null || fxLayer == null) return false;
        if (fromUI == null) return false;

        RectTransform target = GetTarget(type);
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        Image prefab = GetPrefab(type);
        if (prefab == null) return false;

        return true;
    }

    private bool CanFlyExisting(RectTransform existing, RewardType type)
    {
        if (uiCanvas == null || fxLayer == null) return false;
        if (existing == null) return false;

        RectTransform target = GetTarget(type);
        if (target == null) return false;
        if (!target.gameObject.activeInHierarchy) return false;

        return true;
    }

    private RectTransform GetTarget(RewardType type)
    {
        switch (type)
        {
            case RewardType.Gold: return goldTarget;
            case RewardType.Gems: return gemsTarget;
            case RewardType.Part: return unitsPartsTarget;
            default: return null;
        }
    }

    private Image GetPrefab(RewardType type)
    {
        switch (type)
        {
            case RewardType.Gold: return goldIconPrefab;
            case RewardType.Gems: return gemsIconPrefab;
            case RewardType.Part: return partIconPrefab;
            default: return null;
        }
    }

    private IEnumerator SpawnBurstAndWait(RewardType type, Vector2 from, Vector2 to, int iconCount, Action<float> onProgress, Action onComplete)
    {
        int finished = 0;
        int spawned = 0;

        for (int i = 0; i < iconCount; i++)
        {
            Image prefab = GetPrefab(type);
            if (prefab == null) break;

            Image img = Instantiate(prefab, fxLayer);
            RectTransform rt = img.rectTransform;

            Vector2 scatter = UnityEngine.Random.insideUnitCircle * scatterRadius;
            Vector2 start = from + scatter;

            rt.anchoredPosition = start;
            rt.localScale = new Vector3(0.5f, 0.5f, 0.5f);

            spawned++;
            StartCoroutine(FlyRoutine(rt, start, to, () => finished++));

            if (spawnInterval > 0f)
                yield return new WaitForSecondsRealtime(spawnInterval);
            else
                yield return null;
        }

        while (finished < spawned)
        {
            float p = spawned <= 0 ? 1f : (float)finished / spawned;
            onProgress?.Invoke(p);
            yield return null;
        }

        onProgress?.Invoke(1f);
        onComplete?.Invoke();
    }

    private IEnumerator FlyExistingRoutine(RewardType type, RectTransform existing, Action onComplete)
    {
        RectTransform target = GetTarget(type);

        RectTransform oldParent = existing.parent as RectTransform;
        int oldSiblingIndex = existing.GetSiblingIndex();

        GameObject placeholderGO = new GameObject("FX_Placeholder", typeof(RectTransform), typeof(LayoutElement));
        RectTransform placeholder = placeholderGO.GetComponent<RectTransform>();
        placeholder.SetParent(oldParent, false);
        placeholder.SetSiblingIndex(oldSiblingIndex);

        LayoutElement le = placeholderGO.GetComponent<LayoutElement>();
        le.preferredWidth = existing.rect.width;
        le.preferredHeight = existing.rect.height;

        existing.SetParent(fxLayer, worldPositionStays: true);

        Vector2 start = WorldToCanvasPoint(existing.position);
        Vector2 end = WorldToCanvasPoint(target.position);

        existing.anchoredPosition = start;

        float t = 0f;
        Vector2 mid = (start + end) * 0.5f + new Vector2(0f, arcHeight);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, flyDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));

            Vector2 a = Vector2.Lerp(start, mid, k);
            Vector2 b = Vector2.Lerp(mid, end, k);
            existing.anchoredPosition = Vector2.Lerp(a, b, k);

            float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.15f;
            existing.localScale = new Vector3(s, s, 1f);

            yield return null;
        }

        Destroy(existing.gameObject);
        Destroy(placeholderGO);

        onComplete?.Invoke();
    }

    private IEnumerator FlyRoutine(RectTransform rt, Vector2 start, Vector2 end, Action onArrive)
    {
        float t = 0f;
        Vector2 mid = (start + end) * 0.5f + new Vector2(0f, arcHeight);

        while (t < 1f)
        {
            t += Time.unscaledDeltaTime / Mathf.Max(0.01f, flyDuration);
            float k = ease.Evaluate(Mathf.Clamp01(t));

            Vector2 a = Vector2.Lerp(start, mid, k);
            Vector2 b = Vector2.Lerp(mid, end, k);
            rt.anchoredPosition = Vector2.Lerp(a, b, k);

            float s = 1f + Mathf.Sin(k * Mathf.PI) * 0.15f;
            rt.localScale = new Vector3(s, s, 1f);

            yield return null;
        }

        Destroy(rt.gameObject);
        onArrive?.Invoke();
    }

    private Vector2 WorldToCanvasPoint(Vector3 worldPos)
    {
        Camera cam = uiCanvas.renderMode == RenderMode.ScreenSpaceOverlay ? null : uiCanvas.worldCamera;
        Vector2 screen = RectTransformUtility.WorldToScreenPoint(cam, worldPos);

        RectTransform canvasRT = (RectTransform)uiCanvas.transform;
        RectTransformUtility.ScreenPointToLocalPointInRectangle(canvasRT, screen, cam, out Vector2 localPoint);

        return localPoint;
    }
}
