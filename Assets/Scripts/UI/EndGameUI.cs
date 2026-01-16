using System.Collections;
using UnityEngine;
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


    private void Awake()
    {
        Instance = this;
        panel.alpha = 0f;
        panel.interactable = false;
        panel.blocksRaycasts = false;
    }

    public void ShowWinScreen(int goldEarned, int goldEarnedFromRounds,  int totalGold)
    {
        titleText.text = "YOU WON!";

        if (goldEarnedText != null)
            goldEarnedText.text = $"Level completed:\n +{goldEarned} Gold";

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = $"Finish all rounds:\n +{goldEarnedFromRounds} Gold";

        if (totalGoldText != null)
            totalGoldText.text = $"Total gold:\n {totalGold} Gold!";

        StartCoroutine(FadeIn());
    }


    public void ShowLoseScreen()
    {
        titleText.text = "YOU LOST!";

        if (goldEarnedText != null)
            goldEarnedText.text = "";

        if (goldEarnedFromRoundsText != null)
            goldEarnedFromRoundsText.text = "";

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
        SceneManager.LoadScene(0);
    }

    private void OnDisable()
    {
        StopAllCoroutines();
    }
}
