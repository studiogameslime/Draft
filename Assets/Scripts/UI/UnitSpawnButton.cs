using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitSpawnButton : MonoBehaviour
{
    public Image iconImage;  // drag the Image from the prefab here in the Inspector
    public TMP_Text unitName;
    private UnitDefinition data;
    private UnitSelectionUI selectionUI;
    

    // Initialize button with data and callback target
    public void Init(UnitDefinition def, UnitSelectionUI ui)
    {
        data = def;
        selectionUI = ui;
        unitName.text = def.displayName;

        if (iconImage != null)
        {
            iconImage.sprite = def.icon;
            iconImage.SetNativeSize();
        }
    }

    // Called from the Button component OnClick()
    public void OnClick()
    {
        if (selectionUI != null && data != null)
        {
            selectionUI.OnUnitChosen(data);
        }
    }
}
