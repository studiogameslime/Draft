using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIGoldShineFull : MonoBehaviour
{
    [Header("References")]
    public Image goldIcon;
    public Image shineImage;

    [Header("Timing")]
    public float interval = 3f;
    public float shineDuration = 0.6f;

    [Header("Shine Movement")]
    public float startX = -120f;
    public float endX = 120f;

    [Header("Shine Visual")]
    public float maxShineAlpha = 0.8f;
    public float shineScale = 1.2f;

    [Header("Icon Glow")]
    public Color glowColor = new Color(1f, 0.95f, 0.7f);
    public float glowIntensity = 0.25f;

    RectTransform shineRect;
    Vector3 shineBaseScale;
    Color iconOriginalColor;

    void Start()
    {
        shineRect = shineImage.rectTransform;
        shineBaseScale = shineRect.localScale;
        iconOriginalColor = goldIcon.color;

        shineImage.enabled = false;
        StartCoroutine(ShineLoop());
    }

    IEnumerator ShineLoop()
    {
        while (true)
        {
            yield return new WaitForSeconds(interval);
            yield return StartCoroutine(PlayShine());
        }
    }

    IEnumerator PlayShine()
    {
        shineImage.enabled = true;

        float t = 0f;
        while (t < shineDuration)
        {
            t += Time.deltaTime;
            float progress = t / shineDuration;

            // movment
            float x = Mathf.Lerp(startX, endX, progress);
            shineRect.anchoredPosition = new Vector2(x, 0);

            // Alpha (Fade In / Out)
            float alpha = Mathf.Sin(progress * Mathf.PI) * maxShineAlpha;
            SetShineAlpha(alpha);

            // Scale
            float scale = Mathf.Lerp(1f, shineScale, Mathf.Sin(progress * Mathf.PI));
            shineRect.localScale = shineBaseScale * scale;

            // Glow 
            goldIcon.color = Color.Lerp(
                iconOriginalColor,
                glowColor,
                Mathf.Sin(progress * Mathf.PI) * glowIntensity
            );

            yield return null;
        }

        // Reset
        shineImage.enabled = false;
        shineRect.localScale = shineBaseScale;
        goldIcon.color = iconOriginalColor;
    }

    void SetShineAlpha(float a)
    {
        Color c = shineImage.color;
        c.a = a;
        shineImage.color = c;
    }
}