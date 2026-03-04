using UnityEngine;
using UnityEngine.UI;

/// <summary>
/// Clickable turtle that rewards a chest after watching a rewarded ad.
/// Appears every time the home screen loads. Disappears after claiming.
/// </summary>
public class TurtleAdChest : MonoBehaviour
{
    [Header("Reward")]
    [SerializeField] private ChestDefinition chestReward;

    [Header("Chest Icon (child visual)")]
    [SerializeField] private GameObject chestIcon;

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
            _button = gameObject.AddComponent<Button>();

        _button.onClick.AddListener(OnTurtleClicked);
    }

    private void OnEnable()
    {
        if (_button) _button.interactable = true;
        if (chestIcon) chestIcon.SetActive(true);
    }

    private void OnTurtleClicked()
    {
        if (chestReward == null || StoreItemChestPanel.Instance == null) return;

        StoreItemChestPanel.Instance.ShowWithAd(chestReward, () =>
        {
            gameObject.SetActive(false);
        });
    }
}
