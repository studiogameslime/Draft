using UnityEngine;
using UnityEngine.UI;

public class ReplaceModeOverlayController : MonoBehaviour
{
    public static ReplaceModeOverlayController Instance;

    [Header("Overlays")]
    [SerializeField] private GameObject collectionOverlay;
    [SerializeField] private GameObject deckOverlay;

    [SerializeField] private Button collectionOverlayButton;
    [SerializeField] private Button deckOverlayButton;

    private void Awake()
    {
        Instance = this;

        if (collectionOverlayButton != null)
            collectionOverlayButton.onClick.AddListener(OnOverlayClicked);

        if (deckOverlayButton != null)
            deckOverlayButton.onClick.AddListener(OnOverlayClicked);

        Hide();
    }

    public void Show()
    {
        if (collectionOverlay != null)
            collectionOverlay.SetActive(true);

        if (deckOverlay != null)
            deckOverlay.SetActive(true);
    }

    public void Hide()
    {
        if (collectionOverlay != null)
            collectionOverlay.SetActive(false);

        if (deckOverlay != null)
            deckOverlay.SetActive(false);
    }

    private void OnOverlayClicked()
    {
        if (UnitsDeckManager.Instance != null)
            UnitsDeckManager.Instance.CancelReplaceMode();
    }
}
