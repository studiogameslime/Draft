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
    [SerializeField] private Image progressBarFill;
    [SerializeField] private TMP_Text loadingText;

    [Header("Visual Feel")]
    [SerializeField] private float capBeforeActivation = 0.92f;
    [SerializeField] private float smoothTime = 0.18f;
    [SerializeField] private float dotSpeed = 0.25f;

    float visual;
    float vel;

    IEnumerator Start()
    {
        float startT = Time.unscaledTime;
        visual = 0f;
        if (progressBarFill) progressBarFill.fillAmount = 0f;

        // --- Step 1: Load HomeScreen ADDITIVELY (loading scene stays active & visible) ---
        Debug.Log("[LOADING] Loading HomeScreen additively for manager init");
        var homeOp = SceneManager.LoadSceneAsync(homeSceneName, LoadSceneMode.Additive);
        homeOp.allowSceneActivation = false;

        while (homeOp.progress < 0.9f)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);
            float loadProgress = Mathf.Clamp01(homeOp.progress / 0.9f);
            float timeProgress = Mathf.Clamp01(t / Mathf.Max(0.01f, minShowSeconds));
            StepVisual(Mathf.Min(loadProgress, timeProgress) * capBeforeActivation);
            yield return null;
        }

        // Activate HomeScreen additively - managers init via HomeBootstrapper.Awake
        homeOp.allowSceneActivation = true;
        while (!homeOp.isDone)
            yield return null;

        // Immediately disable all root GameObjects in HomeScreen so nothing renders
        var homeScene = SceneManager.GetSceneByName(homeSceneName);
        if (homeScene.IsValid())
        {
            foreach (var root in homeScene.GetRootGameObjects())
                root.SetActive(false);
        }

        Debug.Log("[LOADING] HomeScreen loaded additively, managers initialized, roots disabled");

        // --- Step 2: Check if tutorial needed ---
        bool needsTutorial = GameData.Instance != null
                          && GameData.Instance.IsReady
                          && !GameData.Instance.Save.tutorialComplete;

        string finalScene;
        if (needsTutorial)
        {
            finalScene = "1-1";
            Debug.Log("[LOADING] Tutorial not complete -> final scene is 1-1");
        }
        else
        {
            finalScene = null;
            Debug.Log("[LOADING] Tutorial complete -> final scene is HomeScreen (already loaded)");
        }

        // --- Step 3: If tutorial, load 1-1 while loading screen is still visible ---
        AsyncOperation finalOp = null;
        if (finalScene != null)
        {
            finalOp = SceneManager.LoadSceneAsync(finalScene, LoadSceneMode.Additive);
            finalOp.allowSceneActivation = false;

            while (finalOp.progress < 0.9f)
            {
                float t = Time.unscaledTime - startT;
                UpdateLoadingText(t);
                StepVisual(capBeforeActivation);
                yield return null;
            }
        }

        // --- Step 4: Wait for min show time ---
        while (Time.unscaledTime - startT < minShowSeconds)
        {
            float t = Time.unscaledTime - startT;
            UpdateLoadingText(t);
            StepVisual(capBeforeActivation);
            yield return null;
        }

        // Animate to 100%
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

        // --- Step 5: Everything is ready, swap scenes ---
        if (finalScene != null)
        {
            // Unload HomeScreen (managers survive via DontDestroyOnLoad)
            Debug.Log("[LOADING] Unloading HomeScreen");
            SceneManager.UnloadSceneAsync(homeSceneName);

            // Activate tutorial scene
            finalOp.allowSceneActivation = true;
            while (!finalOp.isDone)
                yield return null;

            // Set 1-1 as the active scene
            var tutorialScene = SceneManager.GetSceneByName(finalScene);
            if (tutorialScene.IsValid())
                SceneManager.SetActiveScene(tutorialScene);

            Debug.Log("[LOADING] 1-1 activated");
        }
        else
        {
            // Re-enable HomeScreen roots and set as active
            if (homeScene.IsValid())
            {
                foreach (var root in homeScene.GetRootGameObjects())
                    root.SetActive(true);
                SceneManager.SetActiveScene(homeScene);
            }
            Debug.Log("[LOADING] HomeScreen activated");
        }

        // Unload the loading scene and destroy this object
        Debug.Log("[LOADING] Removing loading screen");
        var loadingScene = SceneManager.GetSceneByName("LoadingScreen");
        if (loadingScene.IsValid())
            SceneManager.UnloadSceneAsync(loadingScene);
        else
            Destroy(gameObject);
    }

    void StepVisual(float target)
    {
        visual = Mathf.SmoothDamp(visual, target, ref vel, smoothTime);

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
