using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MasteryItemDetailsPopup : MonoBehaviour
{
    [Header("Animator (existing)")]
    [SerializeField] private PopupAnimator popupAnimator;

    [Header("UI")]
    [SerializeField] private Image iconImage; // Assign in Inspector
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text currentValueText;
    [SerializeField] private TMP_Text nextValueText; // Assign in Inspector (new)

    [Header("Buttons")]
    [SerializeField] private Button closeButton;
    [SerializeField] private Button overlayButton;

    private void Awake()
    {
        if (closeButton) closeButton.onClick.AddListener(Close);
        if (overlayButton) overlayButton.onClick.AddListener(Close);
    }

    public void Open(MasteryItemDefinition def, int level, RectTransform fromRect)
    {
        if (def == null || popupAnimator == null) return;

        int maxLevel = def.maxLevel > 0 ? def.maxLevel : 1;
        int currentLevel = Mathf.Clamp(level, 0, maxLevel);

        if (iconImage)
        {
            iconImage.sprite = def.icon;
            iconImage.preserveAspect = true;
            iconImage.gameObject.SetActive(def.icon != null);
        }

        if (titleText) titleText.text = def.displayName;
        if (descriptionText) descriptionText.text = def.description;

        float currentValue = def.GetValueAtLevel(currentLevel);

        if (currentValueText)
        {
            currentValueText.text = def.valueKind == MasteryValueKind.Percent
                ? $"Current Bonus: {currentValue:0.##}%"
                : $"Current Bonus: +{currentValue:0.##}";
        }

        // Next level preview
        if (nextValueText)
        {
            if (currentLevel >= maxLevel)
            {
                nextValueText.text = "Next Bonus: MAX";
            }
            else
            {
                int nextLevel = Mathf.Min(currentLevel + 1, maxLevel);
                float nextValue = def.GetValueAtLevel(nextLevel);

                nextValueText.text = def.valueKind == MasteryValueKind.Percent
                    ? $"Next Bonus: {nextValue:0.##}%"
                    : $"Next Bonus: +{nextValue:0.##}";
            }
        }

        popupAnimator.OpenFromRect(fromRect);
    }

    public void Close()
    {
        if (popupAnimator == null) return;
        popupAnimator.Close();
    }
}
