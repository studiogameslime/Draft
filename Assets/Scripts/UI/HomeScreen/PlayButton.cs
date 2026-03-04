using UnityEngine;

public class PlayButton : MonoBehaviour
{
    [SerializeField] private string fallbackStageId = "1-1";

    public void StartGame()
    {
        // 1) Decide stageId (scene name)
        string stageId = fallbackStageId;

        if (GameData.Instance != null && GameData.Instance.Save != null &&
            !string.IsNullOrEmpty(GameData.Instance.Save.currentStageId))
        {
            stageId = GameData.Instance.Save.currentStageId;
            Debug.Log(stageId);
        }

        // 2) Optional validation (only if LevelsDatabase exists)
        if (LevelsDatabase.Instance != null)
        {
            var def = LevelsDatabase.Instance.GetOrNull(stageId);
            if (def == null)
            {
                Debug.LogError($"[PlayButton] No LevelDefinition found for '{stageId}' (Resources/Levels/{stageId}). Falling back to '{fallbackStageId}'.");
                stageId = fallbackStageId;
            }
        }

        // 3) Save the chosen stageId (so meta UI stays consistent)
        if (GameData.Instance != null && GameData.Instance.Save != null)
        {
            GameData.Instance.Save.currentStageId = stageId;
            GameData.Instance.SaveNow();
        }

        TutorialManager.Instance?.NotifyAction(TutorialAction.PlayPressed);

        // 4) Start your existing flow (animation + load)
        ChapterPreviewController.Instance.PlayWinningAnimationAll();
        StartCoroutine(ChapterPreviewController.Instance.LoadSceneAfterWinning(stageId));
    }
}
