using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using TMPro;

public class LoadingSceneController : MonoBehaviour
{
    [Header("Target Scene (exact Build Settings name)")]
    [SerializeField] private string homeSceneName = "HomeScreen";

    [Header("Timing")]
    [SerializeField] private float minShowSeconds = 5f;
    [SerializeField] private float readyTimeoutSeconds = 20f;

    [Header("UI")]
    [SerializeField] private Image progressBarFill; // Image Type=Filled / Horizontal
    [SerializeField] private TMP_Text loadingText;

    [Header("Visual Feel")]
    [SerializeField] private float capBeforeReady = 0.92f;  // לא 100% לפני Ready
    [SerializeField] private float smoothTime = 0.18f;      // תנועה חלקה יותר
    [SerializeField] private float jitterStep = 0.01f;      // קפיצות "מדורגות"
    [SerializeField] private float dotSpeed = 0.25f;        // נקודות מהירות יותר

    private float visual;
    private float vel;

    private float nextJitterTime;
    private float jitterHold; // עצירה קצרה לפעמים

    IEnumerator Start()
    {
        Scene loadingScene = gameObject.scene;

        // Unscaled time (לא מושפע מ-Time.timeScale)
        float startT = Time.unscaledTime;

        visual = 0f;
        if (progressBarFill) progressBarFill.fillAmount = 0f;

        // טוענים Home additive כדי להשאיר מסך טעינה למעלה
        AsyncOperation op = SceneManager.LoadSceneAsync(homeSceneName, LoadSceneMode.Additive);

        // 1) בזמן טעינת הסצנה (ועם תחושת טעינה)
        while (!op.isDone)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);

            float loadProgress = Mathf.Clamp01(op.progress / 0.9f); // 0..1
            float timeProgress = Mathf.Clamp01(t / Mathf.Max(0.01f, minShowSeconds));

            // יעד אמיתי: לא יותר מהטעינה ולא יותר מהזמן, ועד תקרה לפני Ready
            float target = Mathf.Min(loadProgress, timeProgress) * capBeforeReady;

            StepVisualWithJitter(target);
            yield return null;
        }

        // 2) לחכות ל-HomeBootstrapper.IsReady (עם timeout)
        float readyStart = Time.unscaledTime;
        while (!HomeBootstrapper.IsReady && (Time.unscaledTime - readyStart) < readyTimeoutSeconds)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);

            StepVisualWithJitter(capBeforeReady);
            yield return null;
        }

        // 3) להבטיח מינימום זמן (גם אם Home נטען מהר)
        while (Time.unscaledTime - startT < minShowSeconds)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);

            StepVisualWithJitter(capBeforeReady);
            yield return null;
        }

        // 4) סיום חלק ל-100%
        while (visual < 0.999f)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);

            visual = Mathf.SmoothDamp(visual, 1f, ref vel, 0.10f);
            visual = Mathf.Clamp01(visual);

            if (progressBarFill) progressBarFill.fillAmount = visual;
            yield return null;
        }

        if (progressBarFill) progressBarFill.fillAmount = 1f;

        // 5) להפוך את Home ל-active
        Scene homeScene = SceneManager.GetSceneByName(homeSceneName);
        if (!homeScene.IsValid())
        {
            Debug.LogError($"LoadingSceneController: Home scene '{homeSceneName}' not found (check Build Settings exact name).");
            yield break;
        }
        SceneManager.SetActiveScene(homeScene);

        // 6) לפרוק את סצנת הטעינה
        yield return SceneManager.UnloadSceneAsync(loadingScene);
    }

    private void StepVisualWithJitter(float target)
    {
        // לפעמים עושים "הולד" קצר כדי שזה יראה טבעי
        float now = Time.unscaledTime;

        if (now >= nextJitterTime)
        {
            nextJitterTime = now + Random.Range(0.08f, 0.18f);

            // פעם בכמה זמן: עצירה קטנה (קלאסי במסכי טעינה)
            if (Random.value < 0.12f)
                jitterHold = Random.Range(0.10f, 0.22f);
            else
                jitterHold = 0f;
        }

        if (jitterHold > 0f)
        {
            jitterHold -= Time.unscaledDeltaTime;
            // בזמן hold – עדיין עושים SmoothDamp קטן, אבל כמעט לא זז
            visual = Mathf.SmoothDamp(visual, Mathf.Min(target, visual + 0.002f), ref vel, smoothTime);
        }
        else
        {
            // Smooth לכיוון היעד
            visual = Mathf.SmoothDamp(visual, target, ref vel, smoothTime);

            // “קפיצה” מדורגת קטנה כל פעם שמתאפשר
            if (Random.value < 0.35f)
                visual += Random.Range(0f, jitterStep);
        }

        // לא לעבור את היעד לפני Ready (מונע 100% מוקדם)
        visual = Mathf.Min(visual, target);
        visual = Mathf.Clamp01(visual);

        if (progressBarFill)
            progressBarFill.fillAmount = visual;
    }

    private void UpdateLoadingText(float t)
    {
        if (!loadingText) return;
        int dots = 1 + Mathf.FloorToInt((t / dotSpeed) % 3f);
        loadingText.text = "Loading" + new string('.', dots);
    }
}
