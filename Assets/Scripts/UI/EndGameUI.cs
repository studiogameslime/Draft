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

    public void ShowWinScreen(int goldEarned, int goldEarnedFromRounds, int totalGold, int xpFromLevel, int xpFromRounds, int totalXp)
    {

        HideLevelUI();

        titleText.text = "VICTORY!";

        goldEarnedSprite.sprite = StyleManager.instance.goldSprite;
        goldEarnedFromRoundsSprite.sprite = StyleManager.instance.goldSprite;

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
            totalXpText.text = $"{totalXp}";

        if (goldEarnedText != null)
            goldEarnedText.text = $"{goldEarned}";

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = $"{goldEarnedFromRounds}";

        if (totalGoldText != null)
            totalGoldText.text = $"{totalGold}";

        StartCoroutine(FadeIn());
    }


    public void ShowLoseScreen(int goldEarnedFromRounds, int xpFromRounds)
    {
        HideLevelUI();

        goldEarnedFromRoundsSprite.sprite = StyleManager.instance.goldSprite;

        titleText.text = "Level Lost!";

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
            totalXpText.text = $"{xpFromRounds}";

        if (goldEarnedText != null)
        {
            goldEarnedSprite.gameObject.SetActive(false);
            goldEarnedText.text = $"Incomplete";
        }

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = $"{goldEarnedFromRounds}";

        if (totalGoldText != null)
            totalGoldText.text = $"{goldEarnedFromRounds}";

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

}
