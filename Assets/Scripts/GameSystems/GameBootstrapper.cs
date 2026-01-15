using UnityEngine;
using System.Collections;

public class GameBootstrapper : MonoBehaviour
{
    [SerializeField] private PlayerPartsInventory playerPartsInventory;

    private IEnumerator Start()
    {
        // Wait until GameData is ready
        while (GameData.Instance == null || GameData.Instance.Save == null)
            yield return null;

        playerPartsInventory.LoadFromGameData();
        Debug.Log("GameBootstrapper: Parts inventory loaded");
    }
}
