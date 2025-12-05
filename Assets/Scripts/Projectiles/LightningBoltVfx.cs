using System.Collections;
using UnityEngine;

[RequireComponent(typeof(LineRenderer))]
public class LightningBoltVfx : MonoBehaviour
{
    public float lifeTime = 0.1f;      // how long the bolt is visible
    public float jaggedness = 0.2f;    // how "noisy" the line is
    public int segments = 8;           // how many points along the line

    private LineRenderer line;

    private void Awake()
    {
        line = GetComponent<LineRenderer>();
    }

    public void Play(Vector3 start, Vector3 end)
    {
        if (line == null)
            line = GetComponent<LineRenderer>();

        // Create a jagged line between start and end
        line.positionCount = segments;
        for (int i = 0; i < segments; i++)
        {
            float t = i / (segments - 1f);
            Vector3 pos = Vector3.Lerp(start, end, t);

            // Add small random offset to make it look like lightning
            Vector2 offset = Random.insideUnitCircle * jaggedness;
            pos += (Vector3)offset;

            line.SetPosition(i, pos);
        }

        StartCoroutine(DestroyAfter());
    }

    private IEnumerator DestroyAfter()
    {
        yield return new WaitForSeconds(lifeTime);
        Destroy(gameObject);
    }
}
