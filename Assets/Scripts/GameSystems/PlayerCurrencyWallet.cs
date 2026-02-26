using System;
using UnityEngine;

public class PlayerCurrencyWallet : MonoBehaviour
{
    public static PlayerCurrencyWallet Instance { get; private set; }

    public int Gold { get; private set; }
    public int Gems { get; private set; }
    public int Scrolls { get; private set; }

    public Action<int> OnGoldChanged;
    public Action<int> OnGemsChanged;
    public Action<int> OnScrollsChanged;

    [Header("Lifetime")]
    [Tooltip("If true, wallet persists between scenes.")]
    [SerializeField] private bool dontDestroyOnLoad = true;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;

        if (dontDestroyOnLoad)
            DontDestroyOnLoad(gameObject);
    }

    private void Start()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null)
        {
            Debug.LogError("PlayerCurrencyWallet: GameData not ready");
            return;
        }

        Gold = GameData.Instance.Save.gold;
        Gems = GameData.Instance.Save.gems;
        Scrolls = GameData.Instance.Save.scrolls;

        // Push initial values to UI
        OnGoldChanged?.Invoke(Gold);
        OnGemsChanged?.Invoke(Gems);
        OnScrollsChanged?.Invoke(Scrolls);

        Debug.Log($"Wallet loaded | Gold={Gold}, Gems={Gems}, Scrolls={Scrolls}");
    }

    // ======================================================
    // GOLD
    // ======================================================
    #region Gold

    public void AddGold(int amount, RectTransform fromUI = null, Action onFinished = null)
    {
        if (amount <= 0) return;

        int startValue = Gold;
        int targetValue = Gold + amount;

        if (RewardFlyFXManager.Instance == null || fromUI == null)
        {
            Gold = targetValue;
            OnGoldChanged?.Invoke(Gold);
            SaveGold();
            return;
        }

        int iconCount = RewardFlyFXManager.Instance.GetIconCountForAmount(amount);

        RewardFlyFXManager.Instance.Play(
            RewardType.Gold,
            fromUI,
            iconCount,
            onProgress: (p) =>
            {
                int preview = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, Mathf.Clamp01(p)));
                OnGoldChanged?.Invoke(preview);
            },
            onComplete: () =>
            {
                Gold = targetValue;
                OnGoldChanged?.Invoke(Gold);
                SaveGold();
                onFinished?.Invoke();
            }
        );
    }

    public void SpendGold(int amount)
    {
        if (amount <= 0 || Gold < amount) return;

        Gold -= amount;
        if (Gold < 0) Gold = 0;

        OnGoldChanged?.Invoke(Gold);
        SaveGold();

        MissionsManager.Instance.ReportAction(MissionAction.SpendGold, amount);
    }

    private void SaveGold()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null) return;

        GameData.Instance.Save.gold = Gold;
        GameData.Instance.SaveNow();
    }

    #endregion

    // ======================================================
    // GEMS
    // ======================================================
    #region Gems

    public void AddGems(int amount, RectTransform fromUI = null, Action onFinished = null)
    {
        if (amount <= 0) return;

        int startValue = Gems;
        int targetValue = Gems + amount;

        if (RewardFlyFXManager.Instance == null || fromUI == null)
        {
            Gems = targetValue;
            OnGemsChanged?.Invoke(Gems);
            SaveGems();
            return;
        }

        int iconCount = RewardFlyFXManager.Instance.GetIconCountForAmount(amount);

        RewardFlyFXManager.Instance.Play(
            RewardType.Gems,
            fromUI,
            iconCount,
            onProgress: (p) =>
            {
                int preview = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, Mathf.Clamp01(p)));
                OnGemsChanged?.Invoke(preview);
            },
            onComplete: () =>
            {
                Gems = targetValue;
                OnGemsChanged?.Invoke(Gems);
                SaveGems();
                onFinished?.Invoke();
            }
        );
    }

    public void SpendGems(int amount)
    {
        if (amount <= 0 || Gems < amount) return;

        Gems -= amount;
        if (Gems < 0) Gems = 0;

        OnGemsChanged?.Invoke(Gems);
        SaveGems();

        MissionsManager.Instance.ReportAction(MissionAction.SpendGems, amount);
    }

    private void SaveGems()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null) return;

        GameData.Instance.Save.gems = Gems;
        GameData.Instance.SaveNow();
    }

    #endregion

    // ======================================================
    // SCROLLS  (same logic as Gold/Gems)
    // ======================================================
    #region Scrolls

    public void AddScrolls(int amount, RectTransform fromUI = null, Action onFinished = null)
    {
        if (amount <= 0) return;

        int startValue = Scrolls;
        int targetValue = Scrolls + amount;

        if (RewardFlyFXManager.Instance == null || fromUI == null)
        {
            Scrolls = targetValue;
            OnScrollsChanged?.Invoke(Scrolls);
            SaveScrolls();
            return;
        }

        int iconCount = RewardFlyFXManager.Instance.GetIconCountForAmount(amount);

        RewardFlyFXManager.Instance.Play(
            RewardType.Scrolls,  
            fromUI,
            iconCount,
            onProgress: (p) =>
            {
                int preview = Mathf.RoundToInt(Mathf.Lerp(startValue, targetValue, Mathf.Clamp01(p)));
                OnScrollsChanged?.Invoke(preview);
            },
            onComplete: () =>
            {
                Scrolls = targetValue;
                OnScrollsChanged?.Invoke(Scrolls);
                SaveScrolls();
                onFinished?.Invoke();
            }
        );
    }

    public bool TrySpendScrolls(int amount)
    {
        if (amount <= 0) return true;
        if (Scrolls < amount) return false;

        Scrolls -= amount;
        if (Scrolls < 0) Scrolls = 0;

        OnScrollsChanged?.Invoke(Scrolls);
        SaveScrolls();

        MissionsManager.Instance.ReportAction(MissionAction.SpendScrolls, amount);

        return true;
    }

    private void SaveScrolls()
    {
        if (GameData.Instance == null || GameData.Instance.Save == null) return;

        GameData.Instance.Save.scrolls = Scrolls;
        GameData.Instance.SaveNow();
    }

    #endregion
}