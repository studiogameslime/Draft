using UnityEngine;
using UnityEngine.UI;
using System.Collections;

public class UIRandomStarSparkles : MonoBehaviour
{
    [Header("Prefab")]
    public Image sparklePrefab;

    [Header("Stars Settings")]
    public int starsCount = 5;
    public float spawnRadius = 60f;

    [Header("Timing")]
    public float minInterval = 0.6f;
    public float maxInterval = 1.5f;
    public float sparkleDuration = 0.5f;

    [Header("Scale")]
    public float minScale = 0.3f;
    public float maxScale = 1f;

    [Header("Alpha")]
    public float maxAlpha = 1f;

    RectTransform parentRect;

    void Start()
    {
        parentRect = GetComponent<RectTransform>();

        for (int i = 0; i < starsCount; i++)
        {
            Image star = Instantiate(sparklePrefab, transform);
            star.enabled = false;

            StartCoroutine(StarLoop(star));
        }
    }

    IEnumerator StarLoop(Image star)
    {
        RectTransform rect = star.rectTransform;
        Vector3 baseScale = rect.localScale;
        Color baseColor = star.color;

        while (true)
        {
            yield return new WaitForSeconds(Random.Range(minInterval, maxInterval));

            // random location around the icon
            Vector2 randomPos = Random.insideUnitCircle * spawnRadius;
            rect.anchoredPosition = randomPos;

            star.enabled = true;

            float t = 0f;
            while (t < sparkleDuration)
            {
                t += Time.deltaTime;
                float progress = t / sparkleDuration;

                float curve = Mathf.Sin(progress * Mathf.PI);

                float scale = Mathf.Lerp(minScale, maxScale, curve);
                rect.localScale = baseScale * scale;

                Color c = baseColor;
                c.a = curve * maxAlpha;
                star.color = c;

                yield return null;
            }

            star.enabled = false;
            rect.localScale = baseScale;
            star.color = baseColor;
        }
    }
}
