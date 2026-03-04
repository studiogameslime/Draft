using UnityEngine;
using UnityEngine.UI;

public class ChestOpenButton : MonoBehaviour
{
    public ChestDefinition chestDefinition;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        _button.onClick.RemoveAllListeners();
        _button.onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        if (chestDefinition == null || StoreItemChestPanel.Instance == null) return;

        StoreItemChestPanel.Instance.ShowWithAd(chestDefinition, () =>
        {
            if (_button) _button.interactable = true;
        });
    }
}
