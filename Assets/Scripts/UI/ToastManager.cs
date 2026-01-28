using UnityEngine;

public class ToastManager : MonoBehaviour
{
    public static ToastManager Instance;

    [Header("Setup")]
    [SerializeField] private FloatingToast toastPrefab;
    [SerializeField] private RectTransform toastParent;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    public void Show(string message)
    {
        if (toastPrefab == null || toastParent == null)
        {
            Debug.LogWarning("[ToastManager] Missing prefab or parent");
            return;
        }

        var toast = Instantiate(toastPrefab, toastParent);
        toast.Play(message);
    }
}
