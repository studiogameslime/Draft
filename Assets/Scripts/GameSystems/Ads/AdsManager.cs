using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using GoogleMobileAds.Api;
using Firebase.Analytics;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Ad Unit Ids")]
    [SerializeField] private string androidRewardedAdUnitId = "ca-app-pub-4452511612073107/3937150001";
    [SerializeField] private string test_androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";
    [SerializeField] private bool useTestAdUnit = true;

    [Header("Behavior")]
    [SerializeField] private bool preloadOnInit = true;
    [SerializeField] private float reloadDelaySeconds = 0.5f;

    [Header("Editor Simulation")]
    [SerializeField] private bool simulateInEditor = true;
    [SerializeField] private float editorAdDuration = 1.5f;
    [SerializeField] private bool editorGrantReward = true;

    private bool _initialized;
    private bool _loadRequestedBeforeInit;

    private RewardedAd _rewardedAd;

    // callbacks for the CURRENT show
    private Action _currentOnReward;
    private Action _currentOnClosed;
    private bool _isShowing;

    // main-thread dispatcher
    private readonly Queue<Action> _mainThread = new Queue<Action>();

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
    }

    private void Update()
    {
        // run queued actions on main thread
        while (_mainThread.Count > 0)
        {
            var a = _mainThread.Dequeue();
            try { a?.Invoke(); }
            catch (Exception e) { Debug.LogException(e); }
        }
    }

    private void EnqueueOnMainThread(Action a)
    {
        if (a == null) return;
        _mainThread.Enqueue(a);
    }

    private void OnDestroy()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        _rewardedAd?.Destroy();
#endif
        _rewardedAd = null;
        if (Instance == this) Instance = null;
    }

    // --------------------
    // PUBLIC API
    // --------------------
    public void Initialize()
    {
        if (_initialized) return;

#if UNITY_EDITOR
        _initialized = true;
        Debug.Log(simulateInEditor
            ? "[AdsManager] Initialize (Editor) - Simulation enabled."
            : "[AdsManager] Initialize (Editor) - Simulation disabled.");
        if (preloadOnInit || _loadRequestedBeforeInit)
            _loadRequestedBeforeInit = false;
        return;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        MobileAds.Initialize(_ =>
        {
            _initialized = true;
            Debug.Log("[AdsManager] AdMob initialized.");
            if (preloadOnInit || _loadRequestedBeforeInit)
            {
                _loadRequestedBeforeInit = false;
                LoadRewardedInternal();
            }
        });
#else
        _initialized = true;
        Debug.Log("[AdsManager] Initialize (Non-Android) - Ads disabled.");
#endif
    }

    public bool IsRewardedReady()
    {
#if UNITY_EDITOR
        return simulateInEditor; // “מוכן” לסימולציה
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
        return false;
#endif
    }

    public void LoadRewarded()
    {
#if UNITY_EDITOR
        // אין באמת טעינה באדיטור. רק מדמים "מוכן".
        if (!_initialized) Initialize();
        return;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_initialized)
        {
            _loadRequestedBeforeInit = true;
            Initialize();
            return;
        }
        LoadRewardedInternal();
#endif
    }

    public IEnumerator WaitForRewardedReady(float timeoutSeconds = 10f)
    {
        float start = Time.unscaledTime;
        while (!IsRewardedReady() && (Time.unscaledTime - start) < timeoutSeconds)
            yield return null;

        if (!IsRewardedReady())
            Debug.LogWarning("[AdsManager] Timeout waiting for rewarded to be ready.");
    }

    /// <summary>
    /// onReward נקרא רק אם המשתמש קיבל reward.
    /// onClosed נקרא תמיד כשהמודעה נסגרת (גם אם אין reward / גם אם נכשל).
    /// </summary>
    public bool ShowRewarded(AdRewardType rewardType, Action onReward, Action onClosed = null)
    {
        if (!_initialized)
        {
            Initialize();
            Debug.LogWarning("[AdsManager] ShowRewarded called before initialized.");
            return false;
        }

        if (_isShowing)
        {
            Debug.LogWarning("[AdsManager] Already showing an ad.");
            return false;
        }
        FirebaseAnalytics.LogEvent("watch_ad", new Parameter("reward_type", rewardType.ToString()));

#if UNITY_EDITOR
        if (!simulateInEditor)
        {
            Debug.LogWarning("[AdsManager] Editor simulation is OFF.");
            return false;
        }

        _isShowing = true;
        _currentOnReward = onReward;
        _currentOnClosed = onClosed;


        StartCoroutine(EditorSimRoutine());
        return true;
#endif

#if UNITY_ANDROID && !UNITY_EDITOR
        if (!IsRewardedReady())
        {
            Debug.LogWarning("[AdsManager] Rewarded not ready. Loading...");
            LoadRewarded();
            return false;
        }

        _isShowing = true;
        _currentOnReward = onReward;
        _currentOnClosed = onClosed;

        var ad = _rewardedAd; // local ref
        Debug.Log("[AdsManager] Showing rewarded ad...");

        ad.Show(_ =>
        {
            Debug.Log("[AdsManager] Reward granted.");
            // לא נוגעים ב-UI פה, רק קוראים reward callback (על main thread)
            EnqueueOnMainThread(() => _currentOnReward?.Invoke());
        });

        return true;
#else
        return false;
#endif
    }

#if UNITY_EDITOR
    private IEnumerator EditorSimRoutine()
    {
        Debug.Log("[AdsManager] (Editor) Simulating rewarded ad...");
        yield return new WaitForSecondsRealtime(editorAdDuration);

        if (editorGrantReward)
        {
            Debug.Log("[AdsManager] (Editor) Simulated reward granted.");
            EnqueueOnMainThread(() => _currentOnReward?.Invoke());
        }

        Debug.Log("[AdsManager] (Editor) Simulated ad closed.");
        EnqueueOnMainThread(FinishShowAndNotifyClosed);
    }
#endif

    // --------------------
    // INTERNAL (Android)
    // --------------------
#if UNITY_ANDROID && !UNITY_EDITOR
    private void LoadRewardedInternal()
    {
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        string adUnitId = useTestAdUnit ? test_androidRewardedAdUnitId : androidRewardedAdUnitId;
        Debug.Log("[AdsManager] Loading rewarded...");
        var request = new AdRequest();

        RewardedAd.Load(adUnitId, request, (ad, error) =>
        {
            if (error != null || ad == null)
            {
                Debug.LogWarning($"[AdsManager] Rewarded failed to load: {error}");
                return;
            }

            _rewardedAd = ad;
            HookRewardedEvents(_rewardedAd);
            Debug.Log("[AdsManager] Rewarded loaded (ready).");
        });
    }

    private void HookRewardedEvents(RewardedAd ad)
    {
        ad.OnAdFullScreenContentClosed += () =>
        {
            Debug.Log("[AdsManager] Rewarded closed.");
            EnqueueOnMainThread(FinishShowAndNotifyClosed);
            Invoke(nameof(LoadRewardedInternal), reloadDelaySeconds);
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogWarning($"[AdsManager] Rewarded fullscreen failed: {error}");
            EnqueueOnMainThread(FinishShowAndNotifyClosed);
            Invoke(nameof(LoadRewardedInternal), reloadDelaySeconds);
        };
    }
#endif

    private void FinishShowAndNotifyClosed()
    {
        _isShowing = false;

        var closed = _currentOnClosed;
        _currentOnClosed = null;
        _currentOnReward = null;

        closed?.Invoke();
    }
}
