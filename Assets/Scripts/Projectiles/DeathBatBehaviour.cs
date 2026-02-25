using System.Collections;
using UnityEngine;

// This script handles the natural flight path of the death bat:
// 1. Rises up from the Vampire's death position.
// 2. Swoops in a curved motion toward the wall.
public class DeathBatBehaviour : MonoBehaviour
{
    [Header("Phase 1: Rise")]
    public float riseHeight = 1.5f;
    public float riseDuration = 0.4f;

    [Header("Phase 2: Swoop to Wall")]
    public float flightSpeed = 5f;
    public float curveHeight = 1.0f; // Extra height for the "turn" arc
    public float arrivalDistance = 0.2f;

    [Header("Damage")]
    public int damageToWall = 10;

    private WallHealth targetWall;
    private NinjaGhostTrail ghostTrail;
    private SpriteRenderer spriteRenderer;

    private void Start()
    {
        targetWall = FindFirstObjectByType<WallHealth>();
        ghostTrail = GetComponent<NinjaGhostTrail>();
        spriteRenderer = GetComponent<SpriteRenderer>();

        if (ghostTrail != null)
            ghostTrail.StartTrail();

        if (targetWall != null)
        {
            StartCoroutine(FlightRoutine());
        }
        else
        {
            Destroy(gameObject);
        }
    }

    private IEnumerator FlightRoutine()
    {
        Vector3 startPos = transform.position;
        Vector3 peakPos = startPos + Vector3.up * riseHeight;

        // --- STEP 1: RISE UP ---
        float elapsed = 0f;
        while (elapsed < riseDuration)
        {
            elapsed += Time.deltaTime;
            float t = elapsed / riseDuration;
            // Smoothly lerp to the peak
            transform.position = Vector3.Lerp(startPos, peakPos, Mathf.SmoothStep(0, 1, t));
            yield return null;
        }

        // --- STEP 2: CURVED SWOOP TO WALL ---
        Vector3 currentPos = transform.position;
        Vector3 wallPos = targetWall.transform.position;

        // Calculate duration based on distance and speed
        float distance = Vector3.Distance(currentPos, wallPos);
        float flightDuration = distance / flightSpeed;

        elapsed = 0f;
        while (elapsed < flightDuration)
        {
            if (targetWall == null || targetWall.destroyed) break;

            elapsed += Time.deltaTime;
            float t = elapsed / flightDuration;

            // Update wall position in case it moves (though walls are usually static)
            wallPos = targetWall.transform.position;

            // Quadratic Bezier Curve for the "Turn/Circle" feel
            // p0 = peak, p2 = wall, p1 = mid-point with extra height
            Vector3 p0 = currentPos;
            Vector3 p2 = wallPos;
            Vector3 p1 = (p0 + p2) * 0.5f + Vector3.up * curveHeight;

            // Move along the curve
            Vector3 m1 = Vector3.Lerp(p0, p1, t);
            Vector3 m2 = Vector3.Lerp(p1, p2, t);
            transform.position = Vector3.Lerp(m1, m2, t);

            // Flip sprite based on direction
            if (spriteRenderer != null)
            {
                spriteRenderer.flipX = wallPos.x < transform.position.x;
            }

            // Check arrival
            if (Vector3.Distance(transform.position, p2) <= arrivalDistance)
            {
                HitWall();
                yield break;
            }

            yield return null;
        }

        // Fallback if loop ends
        HitWall();
    }

    private void HitWall()
    {
        if (targetWall != null && !targetWall.destroyed)
        {
            targetWall.TakeDamage(damageToWall);
        }
        Destroy(gameObject);
    }
}