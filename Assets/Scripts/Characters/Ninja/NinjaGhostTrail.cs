using System.Collections;
using UnityEngine;

public class NinjaGhostTrail : MonoBehaviour
{
    [Header("Trail Settings")]
    public float ghostInterval = 0.05f;     // כל כמה זמן ליצור שובל
    public float ghostLife = 0.35f;         // כמה זמן כל שובל חי
    public float ghostAlpha = 0.5f;         // שקיפות התחלתית
    public Color ghostColor = Color.white;  // צבע שובל (אפשר ירקרק/אפרפר)

    private SpriteRenderer sr;
    private bool trailActive = false;

    private void Awake()
    {
        sr = GetComponent<SpriteRenderer>();
    }

    public void StartTrail() => StartCoroutine(TrailRoutine());
    public void StopTrail() => trailActive = false;

    private IEnumerator TrailRoutine()
    {
        trailActive = true;

        while (trailActive)
        {
            CreateGhost();
            yield return new WaitForSeconds(ghostInterval);
        }
    }

    private void CreateGhost()
    {
        GameObject ghost = new GameObject("NinjaGhost");
        SpriteRenderer ghostSR = ghost.AddComponent<SpriteRenderer>();

        ghostSR.sprite = sr.sprite;
        ghostSR.flipX = sr.flipX;
        ghostSR.transform.position = transform.position;
        ghostSR.transform.localScale = transform.localScale;
        ghostSR.color = new Color(1, 1, 1, ghostAlpha);

        Destroy(ghost, ghostLife);

        // fade-out
        StartCoroutine(FadeGhost(ghostSR));
    }

    private IEnumerator FadeGhost(SpriteRenderer ghostSR)
    {
        float t = 0;
        Color c = ghostSR.color;

        while (t < 1f)
        {
            if (!ghostSR) break;
            t += Time.deltaTime / ghostLife;
            ghostSR.color = new Color(c.r, c.g, c.b, Mathf.Lerp(ghostAlpha, 0, t));
            yield return null;
        }
    }
}
