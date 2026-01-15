using UnityEngine;
using UnityEngine.UI;

public class ChestOpenButton : MonoBehaviour
{
    [Header("Config")]
    public ChestDefinition chestDefinition;      // which chest this button will open

    [Header("UI")]
    public ChestOpeningUI chestOpeningUI;        // reference to the main chest opening panel

    private Button _button;

    private void Awake()
    {
        _button = GetComponent<Button>();
        if (_button != null)
        {
            _button.onClick.RemoveAllListeners();
            _button.onClick.AddListener(OnClick);
        }
    }

    private void OnClick()
    {
        if (chestOpeningUI == null)
        {
            Debug.LogWarning("ChestOpenButton: chestOpeningUI is not assigned.");
            return;
        }

        if (chestDefinition == null)
        {
            Debug.LogWarning("ChestOpenButton: chestDefinition is not assigned.");
            return;
        }

        // Show the chest opening screen for this specific chest
        chestOpeningUI.Show(chestDefinition);
    }
}
