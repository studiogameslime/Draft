using UnityEngine;
using TMPro;

public class FloatingDamage : MonoBehaviour
{
    [Header("Spawn Offset")]
    public float startYOffset = 0.5f;

    [Header("Movement")]
    public float moveUpFromStart = 0.8f;
    public float moveDuration = 0.8f;

    [Header("Fade")]
    [Range(0f, 1f)]
    public float fadeStartPercent = 0.6f;   // ????? ??? ?????? ?????? fade

    [Header("Lifetime")]
    public float destroyTime = 1.2f;

    private Vector3 startPos;
    private Vector3 targetPos;
    private float timer;

    private TextMeshPro text;
    private Color originalColor;

    void Start()
    {
        text = GetComponent<TextMeshPro>();
        originalColor = text.color;

        Vector3 basePos = transform.position;
        startPos = basePos + Vector3.up * startYOffset;
        targetPos = startPos + Vector3.up * moveUpFromStart;

        transform.position = startPos;
    }

    void Update()
    {
        timer += Time.deltaTime;

        // Movement
        float moveT = Mathf.Clamp01(timer / moveDuration);
        transform.position = Vector3.Lerp(startPos, targetPos, moveT);

        // Fade
        float lifePercent = timer / destroyTime;
        if (lifePercent >= fadeStartPercent)
        {
            float fadeT = Mathf.InverseLerp(fadeStartPercent, 1f, lifePercent);
            Color c = originalColor;
            c.a = Mathf.Lerp(1f, 0f, fadeT);
            text.color = c;
        }

        if (timer >= destroyTime)
        {
            Destroy(gameObject);
        }
    }
}
