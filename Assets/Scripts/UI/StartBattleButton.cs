using System;
using UnityEngine;
using UnityEngine.UI;

public class StartBattleButton : MonoBehaviour
{
    public static StartBattleButton instance;
    public Button startButton;

    private void Awake()
    {
        instance = this;
        if (startButton == null)
            startButton = GetComponent<Button>();

        if (startButton != null)
            startButton.onClick.AddListener(OnStartBattleClicked);

        startButton.interactable = false;
    }

    private void OnStartBattleClicked()
    {
        if (BattleManager.instance == null)
            return;
        // Start the battle from placed units
        BattleCellSelectionController.Instance.ClearSelection();
        BattleManager.instance.StartRound();

        // Optionally disable the button so it can't be pressed twice
        startButton.interactable = false;
    }

    public void EnableButton()
    {
        startButton.interactable = true;
    }

    internal void DisableButton()
    {
        startButton.interactable = false;
    }
}
