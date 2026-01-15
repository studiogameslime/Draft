using System.Collections;
using UnityEngine;
using TMPro;

public class WaveMessageUI : MonoBehaviour
{
    public static WaveMessageUI Instance;

    [SerializeField] private CanvasGroup panel;
    [SerializeField] private TMP_Text messageText;

    private void Awake()
    {
        Instance = this;
        panel.alpha = 0f;
    }

    public void ShowMessage(string msg, float duration = 2f)
    {
        StartCoroutine(ShowMessageRoutine(msg, duration));
    }

    private IEnumerator ShowMessageRoutine(string msg, float duration)
    {
        messageText.text = msg;

        // Fade In
        for (float t = 0; t < 1; t += Time.deltaTime * 2f)
        {
            panel.alpha = t;
            yield return null;
        }
        panel.alpha = 1f;

        yield return new WaitForSeconds(duration);

        // Fade Out
        for (float t = 1; t > 0; t -= Time.deltaTime * 2f)
        {
            panel.alpha = t;
            yield return null;
        }
        panel.alpha = 0f;
    }
}
