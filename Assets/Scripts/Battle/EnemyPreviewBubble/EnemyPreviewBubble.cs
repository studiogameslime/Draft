using System.Collections;
using TMPro;
using UnityEngine;


public class EnemyPreviewBubble : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private RectTransform headContainer;
    [SerializeField] private TMP_Text countText;

    [Header("Fade In")]
    [SerializeField] private float fadeInDuration = 0.2f;
    [SerializeField] private float fadeInDelay = 0f;

    private CanvasGroup _cg;
    private Coroutine _fadeRoutine;

    private GameObject spawnedHead;

    private void Awake()
    {
        _cg = GetComponent<CanvasGroup>();
        if (_cg == null)
            _cg = gameObject.AddComponent<CanvasGroup>();
    }

    public void Init(UnitDefinition unitDef, int count)
    {
        // text: x3
        if (countText != null)
            countText.text = $"x{Mathf.Max(1, count)}";

        // clean previous head (if any)
        if (spawnedHead != null)
            Destroy(spawnedHead);

        if (unitDef == null || unitDef.headPrefabForBattlePreview == null || headContainer == null)
            return;

        spawnedHead = Instantiate(unitDef.headPrefabForBattlePreview, headContainer);

        // Ensure RectTransform stretch + offsets = 0
        var rt = spawnedHead.GetComponent<RectTransform>();
        if (rt != null)
        {
            rt.anchorMin = Vector2.zero;
            rt.anchorMax = Vector2.one;
            rt.pivot = new Vector2(0.5f, 0.5f);
            rt.anchoredPosition = Vector2.zero;
            rt.offsetMin = Vector2.zero;
            rt.offsetMax = Vector2.zero;
        }
        else
        {
            // fallback if it's not UI (just in case)
            spawnedHead.transform.localPosition = Vector3.zero;
            spawnedHead.transform.localRotation = Quaternion.identity;
            spawnedHead.transform.localScale = Vector3.one;
        }
        PlayFadeIn();
    }

    public void PlayFadeIn()
    {
        if (_cg == null) return;

        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(FadeInRoutine());
    }

    private IEnumerator FadeInRoutine()
    {
        _cg.alpha = 0f;

        if (fadeInDelay > 0f)
            yield return new WaitForSecondsRealtime(fadeInDelay);

        float dur = Mathf.Max(0.01f, fadeInDuration);
        float t = 0f;

        while (t < dur)
        {
            t += Time.unscaledDeltaTime;
            _cg.alpha = Mathf.Clamp01(t / dur);
            yield return null;
        }

        _cg.alpha = 1f;
        _fadeRoutine = null;
    }

}
