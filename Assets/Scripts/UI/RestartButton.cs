using UnityEngine;
using UnityEngine.SceneManagement;

public class RestartButton : MonoBehaviour
{
    public void RestartLevel()
    {
        Debug.Log("Active scene is: " + SceneManager.GetActiveScene().name);

        SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
    }
}
