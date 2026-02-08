using UnityEngine;

public class GameAssetsProvider : MonoBehaviour
{
    public static GameAssetsProvider Instance { get; private set; }

    [SerializeField] private GameAssets assets;
    public GameAssets Assets => assets;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        DontDestroyOnLoad(gameObject);

        if (assets == null)
            Debug.LogError("[GameAssetsProvider] Missing GameAssets reference in inspector!");
    }
}
