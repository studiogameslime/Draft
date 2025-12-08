using UnityEngine;
using TMPro;

public class SoulsManager : MonoBehaviour
{
    public static SoulsManager instance;

    public BattleManager _battleManager;
    public TMP_Text _currentSoulsText;

    [Tooltip("Current souls the player owns")]
    public int soulsLeft;

    private void Awake()
    {
        if (instance != null && instance != this)
        {
            Destroy(gameObject);
            return;
        }
        instance = this;

        if (_battleManager == null)
        {
            _battleManager = FindFirstObjectByType<BattleManager>();
        }
    }

    public void UseSouls(int soulsUsed)
    {
        soulsLeft -= soulsUsed;
        if (soulsLeft < 0) soulsLeft = 0;
        UpdateSoulsCountText();
    }

    public void AddRoundSouls()
    {
        soulsLeft += _battleManager.levelDefinition.rounds[_battleManager.currentRoundIndex].souls;
        UpdateSoulsCountText();
    }

    /// <summary>
    /// Adds souls immediately (used when a soul orb reaches the pool).
    /// </summary>
    public void AddSouls(int amount)
    {
        if (amount <= 0) return;
        soulsLeft += amount;
        UpdateSoulsCountText();
    }

    public bool CheckIfThereIsEnoughSouls(int soulsToCheck)
    {
        return soulsLeft >= soulsToCheck;
    }

    public void UpdateSoulsCountText()
    {
        if (_currentSoulsText != null)
            _currentSoulsText.text = soulsLeft.ToString();
    }
}
