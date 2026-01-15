using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class ChapterPreviewController : MonoBehaviour
{
    public static ChapterPreviewController Instance;

    [Header("Settings")]
    public float winningDuration = 0.8f;   

    private ChapterPreviewUnitAI[] units;

    private void Awake()
    {
        Instance = this;
        units = FindObjectsByType<ChapterPreviewUnitAI>(FindObjectsSortMode.None);
    }

    
    public void PlayWinningAnimationAll()
    {
        foreach (var unit in units)
        {
            if (unit != null)
                unit.PlayWinning(); 
        }
    }

    public IEnumerator LoadSceneAfterWinning(string sceneName)
    {
        PlayWinningAnimationAll();
        yield return new WaitForSeconds(winningDuration);
        UnityEngine.SceneManagement.SceneManager.LoadScene(sceneName);
    }
}
