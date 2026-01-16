using UnityEngine;
using UnityEngine.UI;

public class ChestOpenButton : MonoBehaviour
{
    [Header("Config")]
    public ChestDefinition chestDefinition;

    [Header("UI")]
    public ChestOpeningUI chestOpeningUI;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (chestOpeningUI == null || chestDefinition == null)
        {
            Debug.LogWarning("ChestOpenButton: Missing references.");
            return;
        }

        if (AdsManager.Instance == null)
        {
            Debug.LogWarning("ChestOpenButton: AdsManager not available.");
            return;
        }

        bool started = AdsManager.Instance.ShowRewarded(
            onReward: OpenChest,
            onClosed: OnAdClosed
        );

        if (!started)
        {
            Debug.Log("ChestOpenButton: Rewarded not ready.");
        }
    }

    private void OpenChest()
    {
        Debug.Log("ChestOpenButton: Reward granted - opening chest");

        chestOpeningUI.gameObject.SetActive(true);
        chestOpeningUI.Show(chestDefinition);
    }

    private void OnAdClosed()
    {
        Debug.Log("ChestOpenButton: Ad closed");
    }
}
