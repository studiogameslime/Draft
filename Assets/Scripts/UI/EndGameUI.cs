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

    public void ShowWinScreen(int goldEarned, int goldEarnedFromRounds,  int totalGold, int xpFromLevel, int xpFromRounds, int totalXp)
    {

        HideLevelUI();

        titleText.text = "VICTORY!";

        goldEarnedSprite.sprite = StyleManager.instance.goldSprite;
        goldEarnedFromRoundsSprite.sprite = StyleManager.instance.goldSprite;

        if (xpFromLevelText != null)
            xpFromLevelText.text = $"Level completed:\n +{goldEarned} XP";

        if (xpEarnedFromRoundsText != null)
            xpEarnedFromRoundsText.text = $"Finish all rounds:\n +{goldEarnedFromRounds} XP";

        if (totalXpText != null)
            totalXpText.text = $"Total gold:\n {totalGold} XP!";

        if (goldEarnedText != null)
            goldEarnedText.text = $"Level completed:\n +{goldEarned} Gold";

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = $"Finish all rounds:\n +{goldEarnedFromRounds} Gold";

        if (totalGoldText != null)
            totalGoldText.text = $"Total gold:\n {totalGold} Gold!";

        StartCoroutine(FadeIn());
    }


    public void ShowLoseScreen(int goldEarnedFromRounds, int xpFromRounds)
    {
        HideLevelUI();

        goldEarnedSprite.gameObject.SetActive(false);
        totalGoldSprite.gameObject.SetActive(false);
        goldEarnedFromRoundsSprite.sprite = StyleManager.instance.goldSprite;

        titleText.text = "YOU LOST!";

        if (xpFromLevelText != null)
            xpFromLevelText.text = $"";

        if (xpEarnedFromRoundsText != null)
            xpEarnedFromRoundsText.text = $"Rounds bonus:{xpFromRounds} XP";

        if (totalXpText != null)
            totalXpText.text = $"";

        if (goldEarnedText != null)
            goldEarnedText.text = $"";

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = $"Rounds bonus:{goldEarnedFromRounds} Gold";

        if (totalGoldText != null)
            totalGoldText.text = "";

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
