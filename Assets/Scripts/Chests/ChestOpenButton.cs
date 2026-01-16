using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChestOpenButton : MonoBehaviour
{
    public ChestDefinition chestDefinition;
    public ChestOpeningUI chestOpeningUI;

    private Button _button;
    private bool _rewardGranted;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (!chestOpeningUI || chestDefinition == null) return;
        if (AdsManager.Instance == null) return;

        _rewardGranted = false;
        if (_button) _button.interactable = false;

        // מציגים מודעה
        bool started = AdsManager.Instance.ShowRewarded(
            AdRewardType.FreeChest,
            onReward: () => { _rewardGranted = true; },   // רק מסמנים
            onClosed: () =>
            {
                // חוזרים למשחק -> פותחים רק אחרי סגירה (ובפריים הבא)
                StartCoroutine(AfterAdClosedRoutine());
            });

        if (!started)
        {
            if (_button) _button.interactable = true;
        }
    }

    private IEnumerator AfterAdClosedRoutine()
    {
        yield return null; // פריים אחד אחרי שחזרנו מהמודעה

        if (_rewardGranted)
        {
            chestOpeningUI.gameObject.SetActive(true);
            chestOpeningUI.Show(chestDefinition);
        }

        if (_button) _button.interactable = true;
    }
}
