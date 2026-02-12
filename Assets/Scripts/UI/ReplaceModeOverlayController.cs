using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ReplaceModeOverlayController : MonoBehaviour
{
    public static ReplaceModeOverlayController Instance;

    [Header("Overlays")]
    [SerializeField] private GameObject collectionOverlay;
    [SerializeField] private GameObject deckOverlay;

    [SerializeField] private Button collectionOverlayButton;
    [SerializeField] private Button deckOverlayButton;

    [Header("Fade Settings")]
    [SerializeField] private float fadeDuration = 0.15f;

    private CanvasGroup _collectionCanvasGroup;
    private CanvasGroup _deckCanvasGroup;

    private Coroutine _fadeRoutine;

    private void Awake()
    {
        Instance = this;

        if (collectionOverlayButton != null)
            collectionOverlayButton.onClick.AddListener(OnOverlayClicked);

        if (deckOverlayButton != null)
            deckOverlayButton.onClick.AddListener(OnOverlayClicked);

        if (collectionOverlay != null)
            _collectionCanvasGroup = collectionOverlay.GetComponent<CanvasGroup>();

        if (deckOverlay != null)
            _deckCanvasGroup = deckOverlay.GetComponent<CanvasGroup>();

        HideImmediate();
    }

    // =========================
    // SHOW
    // =========================
    public void Show()
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        if (collectionOverlay != null)
            collectionOverlay.SetActive(true);

        if (deckOverlay != null)
            deckOverlay.SetActive(true);

        _fadeRoutine = StartCoroutine(Fade(0f, 1f));
    }

    // =========================
    // HIDE
    // =========================
    public void Hide()
    {
        if (_fadeRoutine != null)
            StopCoroutine(_fadeRoutine);

        _fadeRoutine = StartCoroutine(Fade(1f, 0f, deactivateOnEnd: true));
    }

    private void HideImmediate()
    {
        SetAlpha(0f);

        if (collectionOverlay != null)
            collectionOverlay.SetActive(false);

        if (deckOverlay != null)
            deckOverlay.SetActive(false);
    }

    // =========================
    // FADE LOGIC
    // =========================
    private IEnumerator Fade(float from, float to, bool deactivateOnEnd = false)
    {
        float t = 0f;

        SetAlpha(from);
        SetInteractable(false);

        while (t < fadeDuration)
        {
            t += Time.deltaTime;
            float alpha = Mathf.Lerp(from, to, t / fadeDuration);
            SetAlpha(alpha);
            yield return null;
        }

        SetAlpha(to);
        SetInteractable(to > 0.9f);

        if (deactivateOnEnd)
        {
            if (collectionOverlay != null)
                collectionOverlay.SetActive(false);

            if (deckOverlay != null)
                deckOverlay.SetActive(false);
        }

        _fadeRoutine = null;
    }

    private void SetAlpha(float value)
    {
        if (_collectionCanvasGroup != null)
            _collectionCanvasGroup.alpha = value;

        if (_deckCanvasGroup != null)
            _deckCanvasGroup.alpha = value;
    }

    private void SetInteractable(bool value)
    {
        if (_collectionCanvasGroup != null)
        {
            _collectionCanvasGroup.interactable = value;
            _collectionCanvasGroup.blocksRaycasts = value;
        }

        if (_deckCanvasGroup != null)
        {
            _deckCanvasGroup.interactable = value;
            _deckCanvasGroup.blocksRaycasts = value;
        }
    }

    // =========================
    // CLICK
    // =========================
    private void OnOverlayClicked()
    {
        if (UnitsDeckManager.Instance != null)
            UnitsDeckManager.Instance.CancelReplaceMode();
    }
}
