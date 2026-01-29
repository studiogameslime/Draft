using UnityEngine;
using UnityEngine.UI;
using TMPro;

/// <summary>
/// Popup for a single upgrade node details:
/// - Shows title/description
/// - Upgrade button unlocks the node (only from this popup)
/// - Close via X or overlay
/// Uses PopupAnimator if assigned (same logic style as other popups).
/// </summary>
public class UnitUpgradeDetailsPopupController : MonoBehaviour
{
    public static UnitUpgradeDetailsPopupController Instance;

    [Header("Root")]
    [SerializeField] private GameObject root;                 // Root
    [SerializeField] private Button closeButton;              // CloseButton
    [SerializeField] private Button overlayCloseButton;       // Overlay (Button) - optional

    [Header("Animator (optional)")]
    [SerializeField] private PopupAnimator popupAnimator;     // optional

    [Header("Content UI")]
    [SerializeField] private TMP_Text titleText;              // Title
    [SerializeField] private TMP_Text descriptionText;        // Description
    [SerializeField] private Button upgradeButton;            // UpgradeButton
    [SerializeField] private TMP_Text upgradeButtonText;      // UpgradeButton/Text (TMP) - optional
    [SerializeField] private Image iconImage;      // UpgradeButton/Text (TMP) - optional

    // runtime context
    private UnitDefinition _unit;
    private UnitUpgradeNodeDefinition _node;
    private UnitProgressData _progress;
    private UpgradeTreeUIController _treeUI;                  // to refresh after upgrade (optional)
    private RectTransform _sourceRect;                        // for anim (optional)

    private void Awake()
    {
        Instance = this;

        if (closeButton != null)
            closeButton.onClick.AddListener(Close);

        if (overlayCloseButton != null)
            overlayCloseButton.onClick.AddListener(Close);

        if (upgradeButton != null)
            upgradeButton.onClick.AddListener(OnUpgradeClicked);

        if (root != null)
            root.SetActive(false);
    }

    /// <summary>
    /// Call this when clicking a node (from UpgradeTreeUIController):
    /// popup.Show(unit, node, treeUI, nodeRect);
    /// </summary>
    public void Show(UnitDefinition unit, UnitUpgradeNodeDefinition node, UpgradeTreeUIController treeUI = null, RectTransform sourceRect = null)
    {
        if (unit == null || node == null)
        {
            Debug.LogWarning("[UnitUpgradeDetailsPopup] Show called with null unit/node.");
            return;
        }

        _unit = unit;
        _node = node;
        _treeUI = treeUI;
        _sourceRect = sourceRect;

        // progress
        _progress = UnitUpgradeProgressService.GetOrCreateUnitProgress(_unit.id);

        if (root != null)
            root.SetActive(true);

        RefreshUI();

        // open animation (optional)
        if (popupAnimator != null && _sourceRect != null)
            popupAnimator.OpenFromRect(_sourceRect, hideButton: false);
    }

    public void Close()
    {
        if (root == null || !root.activeSelf)
            return;

        // close animation (optional)
        if (popupAnimator != null && _sourceRect != null && _sourceRect.gameObject.activeInHierarchy)
        {
            popupAnimator.Close();
        }
        else
        {
            root.SetActive(false);
        }
    }

    private void RefreshUI()
    {
        if (_unit == null || _node == null || _progress == null)
            return;

        if (titleText != null)
            titleText.text = string.IsNullOrEmpty(_node.title) ? _node.nodeId : _node.title;


        if (descriptionText != null)
        {
            // כרגע אין לך תיאור בנוד לפי מה שכתבת, אז נציג placeholder נחמד.
            // בהמשך נוסיף שדה description ל-UnitUpgradeNodeDefinition ונחליף לזה.
            descriptionText.text = _node.description;
        }

        if(iconImage != null)
        {
            iconImage.sprite = _node.icon;
        }

        bool unlocked = UnitUpgradeProgressService.IsUnlocked(_progress, _node.nodeId);
        bool canUnlock = UnitUpgradeProgressService.CanUnlock(_unit, _progress, _node);

        if (upgradeButton != null)
            upgradeButton.interactable = (!unlocked && canUnlock);

        if (upgradeButtonText != null)
        {
            if (unlocked) upgradeButtonText.text = "Unlocked";
            else upgradeButtonText.text = "Upgrade";
        }
    }

    private void OnUpgradeClicked()
    {
        if (_unit == null || _node == null || _progress == null)
            return;

        bool ok = UnitUpgradeProgressService.TryUnlock(_unit, _progress, _node);
        if (!ok)
        {
            RefreshUI();
            return;
        }

        if (_treeUI != null)
        {
            _treeUI.Rebuild();
            FindAnyObjectByType<UnitsCollectionManager>()?.RebuildCollection();
        }

        RefreshUI();

        
    }
}
