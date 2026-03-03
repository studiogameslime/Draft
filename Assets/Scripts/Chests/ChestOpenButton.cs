using System.Collections;
using UnityEngine;
using UnityEngine.UI;

public class ChestOpenButton : MonoBehaviour
{
    public ChestDefinition chestDefinition;

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
        if (chestDefinition == null || StoreItemChestPanel.Instance == null) return;
        if (AdsManager.Instance == null) return;

        _rewardGranted = false;
        if (_button) _button.interactable = false;

        // ������ �����
        bool started = AdsManager.Instance.ShowRewarded(
            AdRewardType.FreeChest,
            onReward: () => { _rewardGranted = true; },   // �� ������
            onClosed: () =>
            {
                // ������ ����� -> ������ �� ���� ����� (������� ���)
                StartCoroutine(AfterAdClosedRoutine());
            });

        if (!started)
        {
            if (_button) _button.interactable = true;
        }
    }

    private IEnumerator AfterAdClosedRoutine()
    {
        yield return null; // ����� ��� ���� ������ �������

        if (_rewardGranted && StoreItemChestPanel.Instance != null)
        {
            StoreItemChestPanel.Instance.Show(chestDefinition);
        }

        if (_button) _button.interactable = true;
    }
}
