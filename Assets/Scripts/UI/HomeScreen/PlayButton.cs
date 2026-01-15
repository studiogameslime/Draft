using UnityEngine;
using UnityEngine.SceneManagement;

public class PlayButton : MonoBehaviour
{
    public string sceneName = "1-3";

    public void StartGame()
    {
        ChapterPreviewController.Instance.PlayWinningAnimationAll();
        StartCoroutine(ChapterPreviewController.Instance.LoadSceneAfterWinning(sceneName));

    }
}
