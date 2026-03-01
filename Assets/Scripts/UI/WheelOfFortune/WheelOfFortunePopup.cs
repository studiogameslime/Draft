using UnityEngine;
using TMPro;
using EasyUI.PickerWheelUI;

public class WheelOfFortunePopup : MonoBehaviour
{
    [Header("Popup Animation")]
    [SerializeField] private PopupAnimator popupAnimator;
    [SerializeField] private GameObject root;

    [Header("UI")]
    [SerializeField] private TMP_Text currentTokenText;

    [Header("Wheel")]
    [SerializeField] private PickerWheel pickerWheel;

    private void Awake()
    {
        root.SetActive(true); // init
    }

    private void Start()
    {
        UpdateTokensCountText();
        root.SetActive(false); // closed by default
    }

    public void UpdateTokensCountText()
    {
        if (currentTokenText == null) return;
        if (GameData.Instance == null || GameData.Instance.Save == null) return;

        currentTokenText.text = GameData.Instance.Save.wheelOfFortuneTokens.ToString();
    }

    public void Close()
    {
        if (popupAnimator == null) return;
        if (pickerWheel != null && pickerWheel.IsSpinning) return;
        popupAnimator.Close();
    }

    public void OpenFromButton(RectTransform buttonRect)
    {
        if (popupAnimator == null)
        {
            Debug.LogWarning("WheelOfFortunePopup: PopupAnimator is not assigned.");
            return;
        }

        root.SetActive(true);

        // Shuffle wheel options on every open
        if (pickerWheel != null)
            pickerWheel.RebuildFromDatabase(randomizeOrder: true);

        UpdateTokensCountText();
        popupAnimator.OpenFromRect(buttonRect, hideButton: true);
    }
}
