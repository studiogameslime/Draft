using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using UnityEngine.SceneManagement;
using Unity.Android.Types;
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
    private int pendingTotalGold;
    private int pendingTotalXp;
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
        string levelName
        )
    {

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
            goldEarnedText.text = $"{goldEarned}";

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = $"{goldEarnedFromRounds}";

        if (totalGoldText != null)
            totalGoldText.text = $"0";

        pendingTotalGold = totalGold;
        pendingTotalXp = totalXp;

        if (victoryPulseRoutine != null)
            StopCoroutine(victoryPulseRoutine);

        victoryPulseRoutine = StartCoroutine(VictoryPulse());

        StartCoroutine(FadeIn());
    }


    public void ShowLoseScreen(int goldEarnedFromRounds, int xpFromRounds)
    {
        HideLevelUI();

        goldEarnedFromRoundsSprite.sprite = StyleManager.instance.goldSprite;
        levelCompletedText.gameObject.SetActive(false);

        titleText.text = "Level Lost!";
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
            goldEarnedText.text = $"Incomplete";
        }

        if (goldEarnedFromRoundsText != null)
        {
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
        SceneManager.LoadScene("HomeScreen");

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

    private IEnumerator CountUpTMP(TMP_Text text, int target, string suffix = "", float duration = 1.2f)
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

        text.transform.localScale = Vector3.one * 1.15f;
        yield return new WaitForSeconds(0.08f);
        text.transform.localScale = Vector3.one;

        text.text = target.ToString() + suffix;
    }

    private IEnumerator PlayCountUps()
    {
        yield return new WaitForSeconds(0.25f);

        if (totalXpText != null)
            StartCoroutine(CountUpTMP(totalXpText, pendingTotalXp));

        if (totalGoldText != null)
            StartCoroutine(CountUpTMP(totalGoldText, pendingTotalGold));
    }

    private IEnumerator VictoryPulse()
    {
        Vector3 baseScale = titleText.transform.localScale;

        while (true)
        {
            float t = (Mathf.Sin(Time.time * victoryPulseSpeed) + 1f) * 0.5f;
            float scale = Mathf.Lerp(1f, victoryPulseScale, t);
            titleText.transform.localScale = baseScale * scale;
            titleText.outlineWidth = Mathf.Lerp(0.2f, 0.35f, t);
            yield return null;
        }
    }
}
