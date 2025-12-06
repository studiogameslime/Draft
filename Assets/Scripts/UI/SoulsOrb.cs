using System.Collections;
using UnityEngine;

public class SoulOrb : MonoBehaviour
{
    [Header("Movement")]
    public float totalTravelTime = 0.5f;
    public float arcHeight = 1.0f;
    public AnimationCurve travelCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Amount")]
    public int soulsAmount = 1;

    private Transform _target;
    private Vector3 _startWorldPos;
    private bool _initialized = false;

    public void Init(Vector3 startPos, Transform target, int amount = 1)
    {
        _startWorldPos = startPos;
        _target = target;
        soulsAmount = Mathf.Max(1, amount);

        transform.position = _startWorldPos;
        _initialized = true;

        StartCoroutine(FlyToTarget());
    }

    private IEnumerator FlyToTarget()
    {
        if (!_initialized || _target == null)
        {
            Destroy(gameObject);
            yield break;
        }

        Vector3 endPos = _target.position;
        float t = 0f;

        Vector3 midPos = (_startWorldPos + endPos) * 0.5f + Vector3.up * arcHeight;

        while (t < 1f)
        {
            t += Time.deltaTime / totalTravelTime;
            float eval = travelCurve.Evaluate(Mathf.Clamp01(t));

            Vector3 p0 = _startWorldPos;
            Vector3 p1 = midPos;
            Vector3 p2 = endPos;

            Vector3 a = Vector3.Lerp(p0, p1, eval);
            Vector3 b = Vector3.Lerp(p1, p2, eval);
            Vector3 pos = Vector3.Lerp(a, b, eval);

            transform.position = pos;

            yield return null;
        }

        if (SoulsManager.instance != null)
        {
            SoulsManager.instance.AddSouls(soulsAmount);
        }

        Destroy(gameObject);
    }
}
