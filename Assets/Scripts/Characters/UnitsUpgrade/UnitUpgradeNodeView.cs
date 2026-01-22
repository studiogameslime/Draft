using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class UnitUpgradeNodeView : MonoBehaviour
{
    [SerializeField] private Button button;
    [SerializeField] private Image icon;
    [SerializeField] private TMP_Text titleText;
    [SerializeField] private GameObject lockOverlay;     
    [SerializeField] private GameObject unlockedCheck;

    public RectTransform Rect => (RectTransform)transform;

    private UnitUpgradeNodeDefinition _node;
    private System.Action<UnitUpgradeNodeDefinition> _onClick;

    public void Bind(
        UnitUpgradeNodeDefinition node,
        bool unlocked,
        bool isAvailable, 
        bool canAfford,  
        System.Action<UnitUpgradeNodeDefinition> onClick)
    {
        _node = node;
        _onClick = onClick;

        if (icon) icon.sprite = node != null ? node.icon : null;
        if (titleText) titleText.text = node != null ? node.title : "";

        if (unlockedCheck) unlockedCheck.SetActive(unlocked);

        if (lockOverlay) lockOverlay.SetActive(!unlocked && !isAvailable);

        if (button)
        {
            button.onClick.RemoveAllListeners();
            button.interactable = true;
            button.onClick.AddListener(() => _onClick?.Invoke(_node));
        }
    }
}
