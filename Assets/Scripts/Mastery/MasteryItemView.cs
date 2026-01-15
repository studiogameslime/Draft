using System;
using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class MasteryItemView : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private Button button; // Assign the card Button here
    [SerializeField] private Image iconImage;
    [SerializeField] private RectTransform iconRect;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text levelText;

    [Header("Rarity Tint")]
    [Tooltip("Assign the card background / frame image you want to tint by rarity.")]
    [SerializeField] private Image rarityTintTargets;

    [Header("Roll Visuals")]
    [Tooltip("Optional. If null, the script will try GetComponent<CanvasGroup>().")]
    [SerializeField] private CanvasGroup canvasGroup;

    private MasteryItemDefinition def;
    private int level;

    // Used to map final pick -> exact view
    public string Id => def != null ? def.id : string.Empty;

    public event Action<MasteryItemDefinition, int, RectTransform> OnClicked;

    private void Awake()
    {
        if (!canvasGroup) canvasGroup = GetComponent<CanvasGroup>();

        if (button != null)
        {
            button.onClick.AddListener(() =>
            {
                OnClicked?.Invoke(def, level, transform as RectTransform);
            });
        }
    }

    public void SetData(
    MasteryItemDefinition definition,
    int newLevel,
    Sprite lockSprite
)
    {
        def = definition;
        level = Mathf.Max(0, newLevel);

        bool isLocked = level <= 0;

        // Title
        if (titleText)
            titleText.text = def != null ? def.displayName : "—";

        // Icon: show lock instead of mastery icon when locked
        if (iconImage)
        {
            if (isLocked && lockSprite != null)
                iconImage.sprite = lockSprite;
            else
                iconImage.sprite = def != null ? def.icon : null;

            iconImage.preserveAspect = true;
        }

        if (iconRect && def != null && level > 0)
            iconRect.localScale = Vector3.one * def.iconScale;

        // Level text: current / max
        int max = (def != null && def.maxLevel > 0) ? def.maxLevel : 1;

        if (levelText)
        {
            levelText.text = $"Lv {Mathf.Min(level, max)}/{max}";
            levelText.gameObject.SetActive(!isLocked);
        }

        // Optional: hide title when locked (as you requested)
        if (titleText)
            titleText.gameObject.SetActive(!isLocked);
    }


    public void SetRarityColor(Color color)
    {
        if (rarityTintTargets == null) return;
        rarityTintTargets.color = color;
    }

    /// <summary>
    /// Dim (fade) the whole card during the roll animation.
    /// </summary>
    public void SetDimmed(bool dim, float dimAlpha)
    {
        if (!canvasGroup) return;
        canvasGroup.alpha = dim ? dimAlpha : 1f;
    }

    /// <summary>
    /// Highlight the card: full alpha + slight scale.
    /// </summary>
    public void SetHighlighted(bool on, float highlightScale, float dimAlpha)
    {
        if (canvasGroup) canvasGroup.alpha = on ? 1f : dimAlpha;
        transform.localScale = on ? Vector3.one * highlightScale : Vector3.one;
    }
}
