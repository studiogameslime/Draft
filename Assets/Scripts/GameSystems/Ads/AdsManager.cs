using System;
using System.Collections;
using UnityEngine;
using GoogleMobileAds.Api;

public class AdsManager : MonoBehaviour
{
    public static AdsManager Instance { get; private set; }

    [Header("Ad Unit Ids")]
    private string androidRewardedAdUnitId = "ca-app-pub-4452511612073107/3937150001";
    private string test_androidRewardedAdUnitId = "ca-app-pub-3940256099942544/5224354917";

    [Header("Behavior")]
    [SerializeField] private bool preloadOnInit = true;
    [SerializeField] private float reloadDelaySeconds = 0.5f;

    private bool _initialized;
    private bool _loadRequestedBeforeInit;
    private RewardedAd _rewardedAd;

    private void Awake()
    {
        if (Instance != null && Instance != this) { Destroy(gameObject); return; }
        Instance = this;
        DontDestroyOnLoad(gameObject);
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
        Debug.Log("[AdsManager] Initialize (Editor/Non-Android) - Ads disabled.");
#endif
    }

    public bool IsRewardedReady()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        return _rewardedAd != null && _rewardedAd.CanShowAd();
#else
        return false;
#endif
    }

    public void LoadRewarded()
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_initialized)
        {
            _loadRequestedBeforeInit = true;
            Initialize();
            return;
        }

        LoadRewardedInternal();
#else
        // Editor: לא טוענים Ads
#endif
    }

    /// <summary>
    /// HomeBootstrapper יכול לחכות לזה לפני IsReady
    /// </summary>
    public IEnumerator WaitForRewardedReady(float timeoutSeconds = 10f)
    {
        float start = Time.unscaledTime;
        while (!IsRewardedReady() && (Time.unscaledTime - start) < timeoutSeconds)
            yield return null;

        if (!IsRewardedReady())
            Debug.LogWarning("[AdsManager] Timeout waiting for rewarded to be ready.");
    }

    /// <summary>
    /// מציג Rewarded. אם המשתמש צפה עד הסוף -> onReward.
    /// בכל מקרה כשהמודעה נסגרת -> onClosed.
    /// </summary>
    public bool ShowRewarded(Action onReward, Action onClosed = null)
    {
#if UNITY_ANDROID && !UNITY_EDITOR
        if (!_initialized)
        {
            Initialize();
            Debug.LogWarning("[AdsManager] ShowRewarded called before initialized. Try again in a moment.");
            return false;
        }

        if (!IsRewardedReady())
        {
            Debug.LogWarning("[AdsManager] Rewarded not ready. Loading...");
            LoadRewarded();
            return false;
        }

        var ad = _rewardedAd; // שומרים reference מקומי
        Debug.Log("[AdsManager] Showing rewarded ad...");

        ad.Show(_ =>
        {
            Debug.Log("[AdsManager] Reward granted.");
            onReward?.Invoke();
        });

        return true;
#else
        Debug.Log("[AdsManager] ShowRewarded skipped (Editor/Non-Android).");
        return false;
#endif
    }

    // --------------------
    // INTERNAL
    // --------------------
#if UNITY_ANDROID && !UNITY_EDITOR
    private void LoadRewardedInternal()
    {
        // מנקים מודעה קודמת אם קיימת
        if (_rewardedAd != null)
        {
            _rewardedAd.Destroy();
            _rewardedAd = null;
        }

        Debug.Log("[AdsManager] Loading rewarded...");
        var request = new AdRequest();

        RewardedAd.Load(test_androidRewardedAdUnitId, request, (ad, error) =>
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
            Debug.Log("[AdsManager] Rewarded closed. Reloading...");
            Invoke(nameof(LoadRewardedInternal), reloadDelaySeconds);
        };

        ad.OnAdFullScreenContentFailed += (AdError error) =>
        {
            Debug.LogWarning($"[AdsManager] Rewarded fullscreen failed: {error}. Reloading...");
            Invoke(nameof(LoadRewardedInternal), reloadDelaySeconds);
        };
    }
#endif
}
