using System.Collections;
using UnityEngine;

public class EnemySpawner : MonoBehaviour
{
    [Header("Spawn Area")]
    [Tooltip("Optional. If assigned, enemies will spawn at a random X between left and right.")]
    [SerializeField] private Transform leftBound;

    [Tooltip("Optional. If assigned, enemies will spawn at a random X between left and right.")]
    [SerializeField] private Transform rightBound;

    [Tooltip("Optional. If assigned, this Y will be used as the spawn Y. If not, spawner's Y is used.")]
    [SerializeField] private Transform spawnYAnchor;

    [Header("Fallback")]
    public Transform spawnPoint;

    [Header("Spawn Visual")]
    [Tooltip("CHANGED: Spawn at alpha 0 and fade to 1 over this duration.")]
    [SerializeField] private float fadeInDuration = 0.5f;

    private void Awake()
    {
        if (spawnPoint == null)
            spawnPoint = transform;

        // CHANGED: If no bounds were assigned, try to auto-detect a BoxCollider2D on this object
        // and use it as a spawn range. This keeps setup easy.
        if (leftBound == null || rightBound == null)
        {
            var box = GetComponent<BoxCollider2D>();
            if (box != null)
            {
                if (leftBound == null)
                {
                    var goL = new GameObject("LeftBound_Runtime");
                    goL.transform.SetParent(transform);
                    leftBound = goL.transform;
                }

                if (rightBound == null)
                {
                    var goR = new GameObject("RightBound_Runtime");
                    goR.transform.SetParent(transform);
                    rightBound = goR.transform;
                }

                Vector2 center = (Vector2)transform.position + box.offset;
                float halfW = box.size.x * 0.5f * Mathf.Abs(transform.lossyScale.x);

                leftBound.position = new Vector3(center.x - halfW, center.y, 0f);
                rightBound.position = new Vector3(center.x + halfW, center.y, 0f);
            }
        }
    }

    public GameObject Spawn(UnitDefinition def, int level)
    {
        Vector3 pos = GetSpawnPosition();
        GameObject go = Instantiate(def.prefab, pos, Quaternion.identity);

        // CHANGED: Spawn transparent, then fade in.
        SetAlpha(go, 0f);
        StartCoroutine(FadeInRoutine(go, fadeInDuration));

        var stats = go.GetComponent<CharacterStats>();
        stats.Init(Team.EnemyTeam, def, level);

        var ai = go.GetComponent<UnitAI>();
        if (ai != null)
            ai.enabled = true;

        return go;
    }

    private Vector3 GetSpawnPosition()
    {
        float y = (spawnYAnchor != null) ? spawnYAnchor.position.y : transform.position.y;

        if (leftBound != null && rightBound != null)
        {
            float minX = Mathf.Min(leftBound.position.x, rightBound.position.x);
            float maxX = Mathf.Max(leftBound.position.x, rightBound.position.x);
            float x = Random.Range(minX, maxX);
            return new Vector3(x, y, 0f);
        }

        return new Vector3(spawnPoint.position.x, spawnPoint.position.y, spawnPoint.position.z);
    }

    // CHANGED: Helpers for fade
    private IEnumerator FadeInRoutine(GameObject go, float duration)
    {
        if (go == null)
            yield break;

        float d = Mathf.Max(0.01f, duration);
        float t = 0f;

        while (t < d && go != null)
        {
            t += Time.deltaTime;
            float a = Mathf.Clamp01(t / d);
            SetAlpha(go, a);
            yield return null;
        }

        if (go != null)
            SetAlpha(go, 1f);
    }

    private void SetAlpha(GameObject go, float alpha)
    {
        if (go == null)
            return;

        alpha = Mathf.Clamp01(alpha);

        // Works for 2D sprites (most common in your project)
        var renderers = go.GetComponentsInChildren<SpriteRenderer>(true);
        for (int i = 0; i < renderers.Length; i++)
        {
            var r = renderers[i];
            Color c = r.color;
            c.a = alpha;
            r.color = c;
        }

        // Optional: if you have UI on enemies, this will also fade it
        var canvasGroups = go.GetComponentsInChildren<CanvasGroup>(true);
        for (int i = 0; i < canvasGroups.Length; i++)
        {
            canvasGroups[i].alpha = alpha;
        }
    }
}
