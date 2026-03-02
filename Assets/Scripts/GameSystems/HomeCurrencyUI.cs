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

    [Header("Texts")]
    [SerializeField] private Sprite goldIcon;
    [SerializeField] private Sprite gemsIcon;
    [SerializeField] private Sprite scrollsIcon;

    [Header("XP Ring")]
    [SerializeField] private Image playerXPFill;

    [Header("XP Fill Animation")]
    [SerializeField] private float fillSmoothTime = 0.20f;
    [SerializeField] private bool animateOnFirstUpdate = false;

    [Header("Currency Lerp")]
    [SerializeField] private float currencyLerpDuration = 0.5f;

    private Coroutine _waitRoutine;

    // fill animation state
    private Coroutine _fillRoutine;
    private float _targetFill = 0f;
    private bool _hasInitialFill = false;

    // currency lerp state
    private Coroutine _goldLerpRoutine;
    private Coroutine _gemsLerpRoutine;
    private Coroutine _scrollsLerpRoutine;
    private int _displayedGold;
    private int _displayedGems;
    private int _displayedScrolls;
    private bool _currencyInitialized;

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

        if (_goldLerpRoutine != null) { StopCoroutine(_goldLerpRoutine); _goldLerpRoutine = null; }
        if (_gemsLerpRoutine != null) { StopCoroutine(_gemsLerpRoutine); _gemsLerpRoutine = null; }
        if (_scrollsLerpRoutine != null) { StopCoroutine(_scrollsLerpRoutine); _scrollsLerpRoutine = null; }

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
        _currencyInitialized = true;

        goldIcon = StyleManager.instance.goldSprite;
        gemsIcon = StyleManager.instance.gemSprite;
        scrollsIcon = StyleManager.instance.scrollSprite;

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
        if (goldText == null) return;

        if (!_currencyInitialized)
        {
            _displayedGold = value;
            goldText.text = value.ToString();
            return;
        }

        if (_goldLerpRoutine != null) StopCoroutine(_goldLerpRoutine);
        _goldLerpRoutine = StartCoroutine(LerpCurrencyText(goldText, _displayedGold, value, v => _displayedGold = v));
    }

    private void UpdateGems(int value)
    {
        if (gemsText == null) return;

        if (!_currencyInitialized)
        {
            _displayedGems = value;
            gemsText.text = value.ToString();
            return;
        }

        if (_gemsLerpRoutine != null) StopCoroutine(_gemsLerpRoutine);
        _gemsLerpRoutine = StartCoroutine(LerpCurrencyText(gemsText, _displayedGems, value, v => _displayedGems = v));
    }

    private void UpdateScrolls(int value)
    {
        if (scrollsText == null) return;

        if (!_currencyInitialized)
        {
            _displayedScrolls = value;
            scrollsText.text = value.ToString();
            return;
        }

        if (_scrollsLerpRoutine != null) StopCoroutine(_scrollsLerpRoutine);
        _scrollsLerpRoutine = StartCoroutine(LerpCurrencyText(scrollsText, _displayedScrolls, value, v => _displayedScrolls = v));
    }

    private IEnumerator LerpCurrencyText(TMP_Text text, int from, int to, System.Action<int> setDisplayed)
    {
        float elapsed = 0f;
        while (elapsed < currencyLerpDuration)
        {
            elapsed += Time.unscaledDeltaTime;
            float t = Mathf.Clamp01(elapsed / currencyLerpDuration);
            float smooth = t * t * (3f - 2f * t);
            int current = Mathf.RoundToInt(Mathf.Lerp(from, to, smooth));
            text.text = current.ToString();
            setDisplayed(current);
            yield return null;
        }

        text.text = to.ToString();
        setDisplayed(to);
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
