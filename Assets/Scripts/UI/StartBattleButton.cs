using UnityEngine;
using UnityEngine.UI;

public class StartBattleButton : MonoBehaviour
{
    public static StartBattleButton instance;
    public BattleManager battleManager;
    public Button startButton;

    private void Awake()
    {
        instance = this;
        if (startButton == null)
            startButton = GetComponent<Button>();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartBattleClicked);

        if (battleManager == null)
        {
            battleManager = FindFirstObjectByType<BattleManager>();
        }
    }

    private void OnStartBattleClicked()
    {
        if (battleManager == null)
            return;

        // Start the battle from placed units
        battleManager.StartBattleFromDeck();

        // Optionally disable the button so it can't be pressed twice
        startButton.interactable = false;
    }

    public void EnableButton()
    {
        startButton.interactable = true;
    }


}
