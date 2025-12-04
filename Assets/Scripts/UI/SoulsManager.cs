using UnityEngine;
using TMPro;
public class SoulsManager : MonoBehaviour
{
    public static SoulsManager instance;
    public BattleManager _battleManager;
    public TMP_Text _currentSoulsText;
    public int soulsLeft;

    private void Awake()
    {
        instance = this;
    }

    public void UseSouls(int soulsUsed)
    {
        soulsLeft -= soulsUsed;
        UpdateSoulsCountText();
    }

    public void AddRoundSouls()
    {
        soulsLeft += _battleManager.levelDefinition.rounds[_battleManager.currentRoundIndex].souls;
        UpdateSoulsCountText();

    }

    public bool CheckIfThereIsEnoughSouls(int soulsToCheck)
    {
        return soulsLeft >= soulsToCheck;
    }

    public void UpdateSoulsCountText()
    {
        _currentSoulsText.text = soulsLeft.ToString();
    }
}
