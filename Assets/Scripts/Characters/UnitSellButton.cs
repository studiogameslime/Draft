using UnityEngine;

public class UnitSellButton : MonoBehaviour
{
    private UnitSpawner _spawner;

    public void Init(UnitSpawner spawner)
    {
        _spawner = spawner;
    }

    public void OnSellClicked()
    {
        if (_spawner == null)
            return;

        _spawner.SellSpawner();
    }
}
