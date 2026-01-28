using System.Collections;
using UnityEngine;
using TMPro;

public class FloatingToast : MonoBehaviour
{
    [Header("Refs")]
    [SerializeField] private TMP_Text text;
    [SerializeField] private CanvasGroup canvasGroup;

    [Header("Animation")]
    [SerializeField] private float duration = 1.0f;
    [SerializeField] private float moveUpDistance = 120f;

    private RectTransform rectTransform;

    private void Awake()
    {
        rectTransform = GetComponent<RectTransform>();

        if (canvasGroup == null)
            canvasGroup = GetComponent<CanvasGroup>();

        if (text == null)
            text = GetComponentInChildren<TMP_Text>();
    }

    public void Play(string message)
    {
        if (text != null)
            text.text = message;

        canvasGroup.alpha = 1f;

        StopAllCoroutines();
        StartCoroutine(Animate());
    }

    private IEnumerator Animate()
    {
        Vector2 startPos = rectTransform.anchoredPosition;
        Vector2 endPos = startPos + Vector2.up * moveUpDistance;

        float t = 0f;

        while (t < duration)
        {
            t += Time.deltaTime;
            float n = Mathf.Clamp01(t / duration);

            rectTransform.anchoredPosition = Vector2.Lerp(startPos, endPos, n);
            canvasGroup.alpha = 1f - n;

            yield return null;
        }

        Destroy(gameObject);
    }
}
