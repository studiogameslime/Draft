using System.Collections;
using UnityEngine;

public class SoulOrb : MonoBehaviour
{
    [Header("Phase 1: Rise up")]
    [Tooltip("כמה יחידות למעלה הנשמה תעלה לפני שתתחיל לנוע למאגר")]
    public float riseHeight = 1.0f;

    [Tooltip("כמה זמן (בשניות) לוקח לעלות למעלה")]
    public float riseDuration = 0.25f;

    [Tooltip("עקומת תנועה לשלב העלייה")]
    public AnimationCurve riseCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

    [Header("Phase 2: Move towards pool")]
    [Tooltip("כמה זמן (בשניות) לוקח לנוע מהשיא אל המאגר")]
    public float moveToTargetDuration = 0.5f;

    [Tooltip("גובה הקשת מעל הקו בין נקודת השיא לבין המאגר")]
    public float arcHeight = 1.0f;

    [Tooltip("עקומת תנועה לשלב ההתקדמות למאגר")]
    public AnimationCurve moveCurve = AnimationCurve.EaseInOut(0, 0, 1, 1);

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

        // ---------- Phase 1: Rise up ----------
        Vector3 riseStart = _startWorldPos;
        Vector3 riseEnd = _startWorldPos + Vector3.up * riseHeight;

        float t = 0f;
        float safeRiseDuration = Mathf.Max(0.01f, riseDuration);

        while (t < 1f)
        {
            t += Time.deltaTime / safeRiseDuration;
            float eval = riseCurve.Evaluate(Mathf.Clamp01(t));
            transform.position = Vector3.Lerp(riseStart, riseEnd, eval);
            yield return null;
        }

        // נקודת ההתחלה של הפאזה השנייה היא אחרי העלייה
        Vector3 phase2Start = riseEnd;
        Vector3 endPos = _target.position;

        // ---------- Phase 2: Move towards pool (Bezier arc) ----------
        t = 0f;
        float safeMoveDuration = Mathf.Max(0.01f, moveToTargetDuration);

        // mid point for bezier (מעל הקו בין ההתחלה למאגר)
        Vector3 midPos = (phase2Start + endPos) * 0.5f + Vector3.up * arcHeight;

        while (t < 1f)
        {
            t += Time.deltaTime / safeMoveDuration;
            float eval = moveCurve.Evaluate(Mathf.Clamp01(t));

            Vector3 p0 = phase2Start;
            Vector3 p1 = midPos;
            Vector3 p2 = endPos;

            Vector3 a = Vector3.Lerp(p0, p1, eval);
            Vector3 b = Vector3.Lerp(p1, p2, eval);
            Vector3 pos = Vector3.Lerp(a, b, eval);

            transform.position = pos;
            yield return null;
        }

        // ---------- Arrived ----------
        if (SoulsManager.instance != null)
        {
            SoulsManager.instance.AddSouls(soulsAmount);
        }

        Destroy(gameObject);
    }
}
