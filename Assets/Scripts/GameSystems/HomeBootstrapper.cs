using System.Collections;
using UnityEngine;

[DefaultExecutionOrder(-10000)]
public class HomeBootstrapper : MonoBehaviour
{
    [System.Serializable]
    public class ManagerEntry
    {
        public string name;
        public GameObject prefab;
    }

    [Header("Managers init order")]
    [SerializeField] private ManagerEntry[] managersInOrder;

    public static bool IsReady { get; private set; }

    private void Awake()
    {
        IsReady = false;

        foreach (var entry in managersInOrder)
        {
            if (entry.prefab == null)
                continue;

            if (IsManagerAlive(entry.prefab))
                continue;

            Instantiate(entry.prefab);
        }
    }

    private IEnumerator Start()
    {
        yield return null;

        // ---- Ads ----
        if (AdsManager.Instance != null)
        {
            AdsManager.Instance.Initialize();
            AdsManager.Instance.LoadRewarded();
            yield return AdsManager.Instance.WaitForRewardedReady();
        }

        IsReady = true;
    }

    private bool IsManagerAlive(GameObject prefab)
    {
        var mono = prefab.GetComponent<MonoBehaviour>();
        if (mono == null) return false;

        var type = mono.GetType();

        var prop = type.GetProperty("Instance");
        if (prop != null)
            return prop.GetValue(null) != null;

        var field = type.GetField("Instance");
        if (field != null)
            return field.GetValue(null) != null;

        return false;
    }
}
