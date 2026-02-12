using UnityEngine;
using UnityEngine.UI;

public class ReplaceModeOverlayController : MonoBehaviour
{
    public static ReplaceModeOverlayController Instance;

    [SerializeField] private GameObject root;
    [SerializeField] private Button overlayButton;

    private void Awake()
    {
        Instance = this;

        if (overlayButton != null)
            overlayButton.onClick.AddListener(OnOverlayClicked);

        if (root != null)
            root.SetActive(false);
    }

    public void Show()
    {
        if (root != null)
        {
            root.SetActive(true);
            root.transform.SetAsLastSibling();
        }

        if (UnitsDeckManager.Instance != null)
            UnitsDeckManager.Instance.transform.SetAsLastSibling();
    }


    public void Hide()
    {
        if (root != null)
            root.SetActive(false);
    }

    private void OnOverlayClicked()
    {
        if (UnitsDeckManager.Instance != null)
        {
            UnitsDeckManager.Instance.CancelReplaceMode();
        }
    }
}
