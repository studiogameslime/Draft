using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BattleUpgradeOptionView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private TMP_Text descriptionText;
    [SerializeField] private TMP_Text soulsCost;
    [SerializeField] private GameObject lockedOverlay;

    public void Bind(UnitUpgradeNodeDefinition node, bool isLocked, System.Action onClick)
    {
        if (icon != null) icon.sprite = node != null ? node.icon : null;
        if (titleText != null) titleText.text = node != null ? node.title : "Empty";
        if (descriptionText != null) descriptionText.text = node != null ? node.description : "";
        if (soulsCost != null) soulsCost.text = node != null ? node.soulsCost.ToString() : null;

        if (lockedOverlay != null)
            lockedOverlay.SetActive(isLocked);

        if (button != null)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = (node != null) && !isLocked;
            if (onClick != null)
                button.onClick.AddListener(() => onClick.Invoke());
        }
    }
}
