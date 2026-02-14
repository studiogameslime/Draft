using UnityEngine;
using TMPro;

public class RoundUIManager : MonoBehaviour
{
    public static RoundUIManager instance;
    [SerializeField] TMP_Text roundText;
    [SerializeField] TMP_Text LevelText;

    private void Awake()
    {
        instance = this;
    }

    public void ChangeRoundText(int currentRound, int maxRound)
    {
        roundText.text = $"{currentRound}/{maxRound}";
    }

    public void ChangeLevelText(string levelName)
    {
        LevelText.text = levelName;
    }


}
