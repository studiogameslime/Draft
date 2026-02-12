using UnityEngine;
using UnityEngine.SceneManagement;

public class PauseManager : MonoBehaviour
{
    public static PauseManager Instance;

    [SerializeField] private GameObject pauseRoot;

    private bool isPaused;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
    }

    private void Start()
    {
        pauseRoot.SetActive(false);
        ResumeGame();
    }

    public void PauseGame()
    {
        if (isPaused)
            return;

        isPaused = true;
        Time.timeScale = 0f;
        pauseRoot.SetActive(true);
    }

    public void ResumeGame()
    {
        isPaused = false;
        Time.timeScale = 1f;
        pauseRoot.SetActive(false);
    }

    public void RestartLevel()
    {
        Time.timeScale = 1f;
        Debug.Log("Active scene is: " + SceneManager.GetActiveScene().name);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }

    public void GoHome()
    {
        Debug.Log("Go Home");
        BattleManager.instance.ExitBattle();
        BattleManager.instance.HandleRoundLost();
        Time.timeScale = 1f;
    }

    private void OnDestroy()
    {
        // in case of scene change
        Time.timeScale = 1f;
    }
}
