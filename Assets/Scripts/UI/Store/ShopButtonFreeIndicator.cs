using UnityEngine;

public class ShopButtonFreeIndicator : MonoBehaviour
{
    [Header("UI")]
    [SerializeField] private GameObject freeIndicatorRoot;

    [Header("Data")]
    [SerializeField] private StoreItemDefinition[] storeItems;

    private void Start()
    {
        if (StoreManager.Instance != null)
        {
            StoreManager.Instance.OnFreePackStateChanged += Refresh;
            Refresh();
        }

    }

    private void OnDestroy()
    {
        if (StoreManager.Instance != null)
        {
            StoreManager.Instance.OnFreePackStateChanged -= Refresh;
        }
    }

    private void Refresh()
    {
        if (StoreManager.Instance == null || freeIndicatorRoot == null)
            return;

        bool hasFree =
            StoreManager.Instance.HasUnclaimedDailyFreePack(storeItems);
        Debug.Log($"hasFree={hasFree}");
        freeIndicatorRoot.SetActive(hasFree);
    }
}

