using UnityEngine;
using UnityEngine.UI;

public class MissionsButton : MonoBehaviour
{
    [SerializeField] private GameObject missionsPanel;
    [SerializeField] private Button upgradeButton;


    //private void Awake()
    //{
    //    if (upgradeButton != null)
    //        upgradeButton.onClick.AddListener(OnUpgradeButtonPressed);
    //}
    public void OpenMissions()
    {
        missionsPanel.SetActive(true);
    }

    public void CloseMissions()
    {
        missionsPanel.SetActive(false);
    }
}

