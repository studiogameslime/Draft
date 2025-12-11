using UnityEngine;
using UnityEngine.UI;

public class ReplaceDeckButton : MonoBehaviour
{
    private UnitDefinition _newUnit;
    private UnitDefinition _oldUnit;
    private UnitsDeckManager _deckManager;

    private void Awake()
    {
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    public void Setup(UnitDefinition newUnit, UnitDefinition oldUnit, UnitsDeckManager deckManager)
    {
        _newUnit = newUnit;
        _oldUnit = oldUnit;
        _deckManager = deckManager;
    }

    private void OnClick()
    {
        Debug.Log(_deckManager);
        _deckManager.ReplaceUnitInDeck(_oldUnit, _newUnit);
        UnitDetailsPopupController.Instance.Close();
    }
}
