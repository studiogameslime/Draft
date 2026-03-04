using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;

public class EndGameUI : MonoBehaviour
{
    public static EndGameUI Instance;

    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text goldEarnedText;
    [SerializeField] private TMP_Text goldEarnedFromRoundsText;
    [SerializeField] private TMP_Text totalGoldText;
    [SerializeField] private TMP_Text xpFromLevelText;
    [SerializeField] private TMP_Text xpEarnedFromRoundsText;
    [SerializeField] private TMP_Text totalXpText;
    [SerializeField] private Image goldEarnedSprite;
    [SerializeField] private Image goldEarnedFromRoundsSprite;
    [SerializeField] private Image totalGoldSprite;
    [SerializeField] private TMP_Text levelCompletedText;

    [Header("Double Gold Ad")]
    [SerializeField] private Button doubleGoldButton;

    private int pendingLevelGold;
    private int pendingRoundsGold;
    private int pendingTotalGold;
    private int pendingTotalXp;
    private int rawGoldEarned;
    private bool doubleGoldClaimed;
    private bool _doubleGoldRewardGranted;
    [SerializeField] private float victoryPulseScale = 1.08f;
    [SerializeField] private float victoryPulseSpeed = 2.2f;

    private Coroutine victoryPulseRoutine;


    [Header("Hide UI")]
    [SerializeField] private GameObject header;
    [SerializeField] private GameObject footer;
    [SerializeField] private GameObject pausePanel;
    [SerializeField] private GameObject waveMessagePanel;
    [SerializeField] private GameObject enemiesPreviewBubbles;

    private void Awake()
    {
        Instance = this;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
    }

    public void ShowWinScreen(
        int goldEarned,
        int goldEarnedFromRounds,
        int totalGold,
        int xpFromLevel,
        int xpFromRounds,
        int totalXp,
        string levelName,
        int rawGold
        )
    {
        rawGoldEarned = rawGold;
        doubleGoldClaimed = false;
        if (doubleGoldButton) doubleGoldButton.gameObject.SetActive(true);

        HideLevelUI();

        titleText.text = "VICTORY!";
        titleText.color = ColorUtility.TryParseHtmlString("#96FF6E", out var c) ? c : Color.white;

        goldEarnedSprite.sprite = StyleManager.instance.goldSprite;
        goldEarnedFromRoundsSprite.sprite = StyleManager.instance.goldSprite;

        levelCompletedText.text = $"Level {levelName} completed!";

        if (xpFromLevelText != null)
        {
            xpFromLevelText.gameObject.SetActive(false);
            xpFromLevelText.text = $"";
        }

        if (xpEarnedFromRoundsText != null)
        {
            xpEarnedFromRoundsText.gameObject.SetActive(false);
            xpEarnedFromRoundsText.text = $"Rounds bonus:{xpFromRounds} XP";
        }

        if (totalXpText != null)
            totalXpText.text = $"0";

        if (goldEarnedText != null)
            goldEarnedText.text = $"0";

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = $"0";

        if (totalGoldText != null)
            totalGoldText.text = $"0";


        pendingLevelGold = goldEarned;
        pendingRoundsGold = goldEarnedFromRounds;
        pendingTotalGold = totalGold;
        pendingTotalXp = totalXp;

        if (victoryPulseRoutine != null)
            StopCoroutine(victoryPulseRoutine);

        victoryPulseRoutine = StartCoroutine(VictoryPulse());

        StartCoroutine(FadeIn());
    }


    public void ShowLoseScreen(int goldEarnedFromRounds, int xpFromRounds)
    {
        if (doubleGoldButton) doubleGoldButton.gameObject.SetActive(false);

        HideLevelUI();

        goldEarnedFromRoundsSprite.sprite = StyleManager.instance.goldSprite;
        levelCompletedText.gameObject.SetActive(false);

        titleText.text = "Defeat!";
        titleText.color = ColorUtility.TryParseHtmlString("#FF7872", out var c) ? c : Color.white;


        if (xpFromLevelText != null)
        {
            xpFromLevelText.gameObject.SetActive(false);
            xpFromLevelText.text = $"";
        }

        if (xpEarnedFromRoundsText != null)
        {
            xpEarnedFromRoundsText.gameObject.SetActive(false);
            xpEarnedFromRoundsText.text = $"";
        }

        if (totalXpText != null)
            totalXpText.text = $"0";

        if (goldEarnedText != null)
        {
            goldEarnedSprite.gameObject.SetActive(false);
            goldEarnedText.color = ColorUtility.TryParseHtmlString("#FF7872", out var e) ? e : Color.white;
            goldEarnedText.text = $"Incomplete";
        }

        if (goldEarnedFromRoundsText != null)
        {
            goldEarnedFromRoundsSprite.gameObject.SetActive(false);
            goldEarnedFromRoundsText.gameObject.SetActive(false);
            goldEarnedFromRoundsText.text = $"";
        }

        if (totalGoldText != null)
            totalGoldText.text = $"0";

        pendingTotalGold = goldEarnedFromRounds;
        pendingTotalXp = xpFromRounds;

        StartCoroutine(FadeIn());
    }


    private IEnumerator FadeIn()
    {
        panel.gameObject.SetActive(true);

        panel.interactable = false;
        panel.blocksRaycasts = false;

        // Fade In
        for (float t = 0; t < 1f; t += Time.deltaTime * 1.5f)
        {
            panel.alpha = t;
            yield return null;
        }
        panel.alpha = 1f;

        panel.interactable = true;
        panel.blocksRaycasts = true;

        StartCoroutine(PlayCountUps());
    }

    public void HideScreen()
    {
        StartCoroutine(FadeOut());
    }

    private IEnumerator FadeOut()
    {
        panel.interactable = false;
        panel.blocksRaycasts = false;

        for (float t = 1f; t > 0f; t -= Time.deltaTime * 1.5f)
        {
            panel.alpha = t;
            yield return null;
        }
        panel.alpha = 0f;

        panel.gameObject.SetActive(false);

        if (victoryPulseRoutine != null)
        {
            StopCoroutine(victoryPulseRoutine);
            victoryPulseRoutine = null;
        }

        titleText.transform.localScale = Vector3.one;
    }

    public void BackToHome()
    {
        TutorialManager.Instance?.NotifyAction(TutorialAction.BackToHome);
        SceneManager.LoadScene("HomeScreen");
    }

    // ============================
    // DOUBLE GOLD (Ad Reward)
    // ============================

    public void OnDoubleGoldClicked()
    {
        if (doubleGoldClaimed) return;

        _doubleGoldRewardGranted = false;

        bool started = AdsManager.Instance.ShowRewarded(
            AdRewardType.DoubleGold,
            onReward: () => { _doubleGoldRewardGranted = true; },
            onClosed: () => { StartCoroutine(AfterDoubleGoldAdClosed()); });

        if (!started)
            return;

        if (doubleGoldButton) doubleGoldButton.interactable = false;
    }

    private IEnumerator AfterDoubleGoldAdClosed()
    {
        yield return null;

        if (_doubleGoldRewardGranted)
        {
            PlayerCurrencyWallet.Instance.AddGold(rawGoldEarned);
            doubleGoldClaimed = true;
        }
        else
        {
            if (doubleGoldButton) doubleGoldButton.interactable = true;
        }

        BackToHome();
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }

    public void HideLevelUI()
    {
        header.gameObject.SetActive(false);
        footer.gameObject.SetActive(false);
        pausePanel.gameObject.SetActive(false);
        waveMessagePanel.gameObject.SetActive(false);
        enemiesPreviewBubbles.gameObject.SetActive(false);
    }

    private IEnumerator CountUpTMP(TMP_Text text, int target, string suffix = "", float duration = 0.8f)
    {
        float time = 0f;
        int start = 0;

        while (time < duration)
        {
            time += Time.deltaTime;

            float t = 1f - Mathf.Pow(1f - time / duration, 3f); // easing
            int value = Mathf.RoundToInt(Mathf.Lerp(start, target, t));

            text.text = value.ToString() + suffix;
            yield return null;
        }

        // pop
        //text.transform.localScale = Vector3.one * 1.15f;
        //yield return new WaitForSeconds(0.08f);
        //text.transform.localScale = Vector3.one;

        text.text = target.ToString() + suffix;
    }

    private IEnumerator PlayCountUps()
    {
        yield return new WaitForSeconds(0.25f);

        // XP
        if (totalXpText != null)
            yield return CountUpTMP(totalXpText, pendingTotalXp);

        // Level gold
        if (goldEarnedText != null && goldEarnedText.text != "Incomplete") 
            yield return CountUpTMP(goldEarnedText, pendingLevelGold);

        // Rounds gold
        if (goldEarnedFromRoundsText != null)
            yield return CountUpTMP(goldEarnedFromRoundsText, pendingRoundsGold);

        // Total gold
        if (totalGoldText != null)
            yield return CountUpTMP(totalGoldText, pendingTotalGold);

    }

    private IEnumerator VictoryPulse()
    {
        Vector3 baseScale = titleText.transform.localScale;

        while (true)
        {
            float t = (Mathf.Sin(Time.time * victoryPulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(1f, victoryPulseScale, t);
            titleText.transform.localScale = baseScale * scale;
            titleText.outlineWidth = Mathf.Lerp(0.2f, 0.30f, t);
            yield return null;
        }
    }
}
