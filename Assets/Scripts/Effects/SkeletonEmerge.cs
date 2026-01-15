using System.Collections;
using UnityEngine;

[DisallowMultipleComponent]
public class SkeletonEmerge : MonoBehaviour
{
    [Header("Emerge Motion")]
    [SerializeField] private float duration = 0.35f;
    [SerializeField] private float startYOffset = 0.25f;   
    [SerializeField] private float overshoot = 1.08f;    
    [SerializeField] private bool disableAIWhileEmerging = true;

    [Header("Optional")]
    [SerializeField] private ParticleSystem dirtParticles; 

    private Vector3 originalPos;
    private Vector3 originalScale;

    private UnitAI ai;

    private void Awake()
    {
        originalPos = transform.position;
        originalScale = transform.localScale;

        ai = GetComponent<UnitAI>();
    }

    private void OnEnable()
    {
        StopAllCoroutines();
    }

    private void Start()
    {
        StartCoroutine(EmergeRoutine());
    }

    private IEnumerator EmergeRoutine()
    {
        if (disableAIWhileEmerging && ai != null)
            ai.enabled = false;

        transform.position = originalPos + Vector3.down * startYOffset;
        transform.localScale = new Vector3(originalScale.x, 0.02f, originalScale.z);

        if (dirtParticles != null)
            dirtParticles.Play();

        float t = 0f;
        while (t < 1f)
        {
            t += Time.deltaTime / Mathf.Max(0.01f, duration);
            float eased = EaseOutCubic(Mathf.Clamp01(t));

            transform.position = Vector3.Lerp(originalPos + Vector3.down * startYOffset, originalPos, eased);

            float yScale = Mathf.Lerp(0.02f, originalScale.y, eased);
            transform.localScale = new Vector3(originalScale.x, yScale, originalScale.z);

            yield return null;
        }

        transform.localScale = new Vector3(originalScale.x, originalScale.y * overshoot, originalScale.z);
        yield return new WaitForSeconds(0.05f);
        transform.localScale = originalScale;

        if (disableAIWhileEmerging && ai != null)
            ai.enabled = true;
    }

    private float EaseOutCubic(float x) => 1f - Mathf.Pow(1f - x, 3f);
}
