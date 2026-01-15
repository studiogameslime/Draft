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

    [Header("UI")]
    [SerializeField] private Image progressBarFill; // Image Type=Filled / Horizontal
    [SerializeField] private TMP_Text loadingText;

    [Header("Visual Feel")]
    [SerializeField] private float capBeforeActivation = 0.92f; // לא 100% לפני האקטיבציה
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float dotSpeed = 0.25f;

    float visual;
    float vel;

    IEnumerator Start()
    {
        float startT = Time.unscaledTime;
        visual = 0f;
        if (progressBarFill) progressBarFill.fillAmount = 0f;

        // SINGLE כדי שהטעינה תישאר עד שמאשרים "אקטיבציה"
        var op = SceneManager.LoadSceneAsync(homeSceneName, LoadSceneMode.Single);
        op.allowSceneActivation = false;

        // Unity מדווח עד ~0.9 עד שמאשרים activation
        while (op.progress < 0.9f)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);

            float loadProgress = Mathf.Clamp01(op.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(t / Mathf.Max(0.01f, minShowSeconds));

            float target = Mathf.Min(loadProgress, timeProgress) * capBeforeActivation;
            StepVisual(target);

            yield return null;
        }

        // להבטיח מינימום זמן מסך טעינה
        while (Time.unscaledTime - startT < minShowSeconds)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);

            StepVisual(capBeforeActivation);
            yield return null;
        }

        // סיום ל-100% ואז כניסה
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

        // עכשיו מפעילים את הסצנה באמת
        op.allowSceneActivation = true;
    }

    void StepVisual(float target)
    {
        visual = Mathf.SmoothDamp(visual, target, ref vel, smoothTime);

        // “קפיצות” קטנות שמרגישות טעינה
        if (Random.value < 0.25f)
            visual += Random.Range(0f, 0.01f);

        visual = Mathf.Min(visual, target);
        visual = Mathf.Clamp01(visual);

        if (progressBarFill)
            progressBarFill.fillAmount = visual;
    }

    void UpdateLoadingText(float t)
    {
        if (!loadingText) return;
        int dots = 1 + Mathf.FloorToInt((t / dotSpeed) % 3f);
        loadingText.text = "Loading" + new string('.', dots);
    }
}
