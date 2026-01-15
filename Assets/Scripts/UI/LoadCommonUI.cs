using UnityEngine;
using UnityEngine.SceneManagement;

public class LoadCommonUI : MonoBehaviour
{
    private void Start()
    {
        if (!SceneManager.GetSceneByName("CommonUI").isLoaded)
        {
            SceneManager.LoadScene("CommonUI", LoadSceneMode.Additive);
        }
    }
}

