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
    [SerializeField] private TMP_Text costText;

    private UnitSelectionUI ownerUI;

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

        if (icon != null)
            icon.sprite = def.icon;

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
}
