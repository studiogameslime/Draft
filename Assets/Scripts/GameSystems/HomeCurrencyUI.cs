using UnityEngine;
using TMPro;
using System.Collections;
using UnityEngine.UI;

public class HomeCurrencyUI : MonoBehaviour
{
    [Header("Texts")]
    [SerializeField] private TMP_Text goldText;
    [SerializeField] private TMP_Text gemsText;
    [SerializeField] private TMP_Text scrollsText;
    [SerializeField] private TMP_Text playerLevelText;

    [Header("XP Ring")]
    [SerializeField] private Image playerXPFill;

    [Header("XP Fill Animation")]
    [SerializeField] private float fillSmoothTime = 0.20f; 
    [SerializeField] private bool animateOnFirstUpdate = false;

    private Coroutine _waitRoutine;

    // fill animation state
    private Coroutine _fillRoutine;
    private float _targetFill = 0f;
    private bool _hasInitialFill = false;

    private void OnEnable()
    {
        _waitRoutine = StartCoroutine(WaitAndSubscribe());
    }

    private void OnDisable()
    {
        if (_waitRoutine != null)
        {
            StopCoroutine(_waitRoutine);
            _waitRoutine = null;
        }

        if (_fillRoutine != null)
        {
            StopCoroutine(_fillRoutine);
            _fillRoutine = null;
        }

        if (PlayerCurrencyWallet.Instance != null)
        {
            PlayerCurrencyWallet.Instance.OnGoldChanged -= UpdateGold;
            PlayerCurrencyWallet.Instance.OnGemsChanged -= UpdateGems;
            PlayerCurrencyWallet.Instance.OnScrollsChanged -= UpdateScrolls;
        }

        if (PlayerXPManager.Instance != null)
        {
            PlayerXPManager.Instance.OnXPChanged -= UpdatePlayerXP;
            PlayerXPManager.Instance.OnLevelChanged -= UpdatePlayerLevel;
        }
    }

    private IEnumerator WaitAndSubscribe()
    {
        while (PlayerCurrencyWallet.Instance == null)
            yield return null;

        var wallet = PlayerCurrencyWallet.Instance;

        wallet.OnGoldChanged -= UpdateGold;
        wallet.OnGoldChanged += UpdateGold;

        wallet.OnGemsChanged -= UpdateGems;
        wallet.OnGemsChanged += UpdateGems;

        wallet.OnScrollsChanged -= UpdateScrolls;
        wallet.OnScrollsChanged += UpdateScrolls;

        UpdateGold(wallet.Gold);
        UpdateGems(wallet.Gems);
        UpdateScrolls(wallet.Scrolls);

        // XP
        var xpManager = PlayerXPManager.Instance;
        if (xpManager != null)
        {
            xpManager.OnXPChanged -= UpdatePlayerXP;
            xpManager.OnXPChanged += UpdatePlayerXP;

            xpManager.OnLevelChanged -= UpdatePlayerLevel;
            xpManager.OnLevelChanged += UpdatePlayerLevel;

            UpdatePlayerLevel(xpManager.currentLevel);
            UpdatePlayerXP(xpManager.currentXP);
        }
    }

    private void UpdateGold(int value)
    {
        if (goldText != null)
            goldText.text = value.ToString();
    }

    private void UpdateGems(int value)
    {
        if (gemsText != null)
            gemsText.text = value.ToString();
    }

    private void UpdateScrolls(int value)
    {
        if (scrollsText != null)
            scrollsText.text = value.ToString();
    }

    private void UpdatePlayerLevel(int level)
    {
        if (playerLevelText != null)
            playerLevelText.text = level.ToString();
    }

    private void UpdatePlayerXP(int currentXP)
    {
        var xpManager = PlayerXPManager.Instance;
        if (xpManager == null || playerXPFill == null)
            return;

        int xpForNextLevel = Mathf.Max(1, xpManager.GetXPForNextLevel());
        float normalized = Mathf.Clamp01((float)currentXP / xpForNextLevel);

        // First time: set instantly (unless you want animation on first)
        if (!_hasInitialFill)
        {
            _hasInitialFill = true;

            if (!animateOnFirstUpdate)
            {
                playerXPFill.fillAmount = normalized;
                _targetFill = normalized;
                return;
            }
        }

        SetFillTargetSmooth(normalized);
    }

    private void SetFillTargetSmooth(float target)
    {
        _targetFill = Mathf.Clamp01(target);

        if (_fillRoutine != null)
            StopCoroutine(_fillRoutine);

        _fillRoutine = StartCoroutine(AnimateFillRoutine(_targetFill));
    }

    private IEnumerator AnimateFillRoutine(float target)
    {
        float start = playerXPFill.fillAmount;

        if (target < start - 0.001f)
        {
            yield return AnimateFillSegment(start, 1f, fillSmoothTime * 0.6f);
            playerXPFill.fillAmount = 0f;
            yield return AnimateFillSegment(0f, target, fillSmoothTime * 0.8f);
        }
        else
        {
            yield return AnimateFillSegment(start, target, fillSmoothTime);
        }

        _fillRoutine = null;
    }

    private IEnumerator AnimateFillSegment(float from, float to, float duration)
    {
        duration = Mathf.Max(0.01f, duration);
        float t = 0f;

        while (t < duration)
        {
            t += Time.unscaledDeltaTime;
            float a = Mathf.Clamp01(t / duration);
            playerXPFill.fillAmount = Mathf.Lerp(from, to, a);
            yield return null;
        }

        playerXPFill.fillAmount = to;
    }
}
