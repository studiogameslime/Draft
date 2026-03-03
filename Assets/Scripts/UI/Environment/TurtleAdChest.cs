using System.Collections;
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
    private bool _rewardGranted;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button == null)
            _button = gameObject.AddComponent<Button>();

        _button.onClick.AddListener(OnTurtleClicked);
    }

    private void OnEnable()
    {
        // Every time the turtle activates (home screen load), make sure it's clickable
        if (_button) _button.interactable = true;
        if (chestIcon) chestIcon.SetActive(true);
    }

    private void OnTurtleClicked()
    {
        if (chestReward == null || AdsManager.Instance == null) return;

        _rewardGranted = false;
        if (_button) _button.interactable = false;

        bool started = AdsManager.Instance.ShowRewarded(
            AdRewardType.FreeChest,
            onReward: () => { _rewardGranted = true; },
            onClosed: () => { StartCoroutine(AfterAdClosed()); }
        );

        // Ad not ready / failed to start — let the player try again
        if (!started)
        {
            if (_button) _button.interactable = true;
        }
    }

    private IEnumerator AfterAdClosed()
    {
        yield return null; // let callbacks settle

        if (_rewardGranted && StoreItemChestPanel.Instance != null)
        {
            StoreItemChestPanel.Instance.Show(chestReward);
            // Hide the turtle after claiming the chest
            gameObject.SetActive(false);
        }
        else
        {
            // Player closed ad without watching — turtle stays
            if (_button) _button.interactable = true;
        }
    }
}
