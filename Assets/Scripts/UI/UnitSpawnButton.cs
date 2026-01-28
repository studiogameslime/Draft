using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// UI button for selecting a unit to place on the grid.
/// Click only (no drag & drop).
/// </summary>
public class UnitSpawnButton : MonoBehaviour
{
    [Header("Unit")]
    public UnitDefinition unitDefinition;

    [Header("UI")]
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private Image background;
    [SerializeField] private TMP_Text costText;

    private UnitSelectionUI ownerUI;
    [SerializeField] private float maxIconSize = 100f;

    private void Awake()
    {
        // Auto-wire basic references to reduce prefab setup mistakes.
        if (button == null)
            button = GetComponent<Button>();

        if (costText == null)
            costText = GetComponentInChildren<TMP_Text>(true);

        if (button != null)
        {
            button.onClick.RemoveListener(OnClicked);
            button.onClick.AddListener(OnClicked);
        }
    }

    /// <summary>
    /// Called right after instantiation by UnitSelectionUI.
    /// </summary>
    public void Init(UnitDefinition def, UnitSelectionUI owner)
    {
        unitDefinition = def;
        ownerUI = owner;

        if (def == null)
            return;

        switch (def.rarity)
        {
            case UnitRarity.Common:
                background.sprite = StyleManager.instance.commonUnitBackground;
                break;
            case UnitRarity.Rare:
                background.sprite = StyleManager.instance.rareUnitBackground;
                break;
            case UnitRarity.Epic:
                background.sprite = StyleManager.instance.epicUnitBackground;
                break;
        }
    

        if (icon != null)
        {
            icon.sprite = def.icon;
            NormalizeIconSize(icon);
        }


        if (costText != null)
            costText.text = def.soulCost.ToString();
    }

    private void OnClicked()
    {
        if (unitDefinition == null)
            return;

        // Forward selection to the placement controller.
        if (BattleCellSelectionController.Instance != null)
            BattleCellSelectionController.Instance.TryPlaceSpawner(unitDefinition);
    }

    public void SetCardInteractable(bool interactable)
    {
        if (button != null)
            button.interactable = interactable;
    }

    private void NormalizeIconSize(Image image)
    {
        if (image == null || image.sprite == null)
            return;

        RectTransform rt = image.rectTransform;

        Vector2 spriteSize = image.sprite.rect.size;
        float maxSide = Mathf.Max(spriteSize.x, spriteSize.y);

        if (maxSide <= 0f)
            return;

        float scale = maxIconSize / maxSide;
        scale = Mathf.Min(scale, 1f); // never upscale, only downscale

        rt.sizeDelta = spriteSize * scale;
    }
}
