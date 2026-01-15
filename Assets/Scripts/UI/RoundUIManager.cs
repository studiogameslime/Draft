using UnityEngine;
using TMPro;

public class RoundUIManager : MonoBehaviour
{
    public static RoundUIManager instance;
    [SerializeField] TMP_Text roundText;

    private void Awake()
    {
        instance = this;
    }

    public void ChangeRoundText(int currentRound, int maxRound)
    {
        roundText.text = $"Round: {currentRound}/{maxRound}";
    }

    
}
