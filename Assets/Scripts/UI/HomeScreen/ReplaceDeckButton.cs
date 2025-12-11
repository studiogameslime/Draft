using UnityEngine;
using UnityEngine.UI;

public class ReplaceDeckButton : MonoBehaviour
{
    private UnitDefinition _newUnit;
    private UnitDefinition _oldUnit;
    private UnitsDeckManager _deckManager;

    public void Setup(UnitDefinition newUnit, UnitDefinition oldUnit, UnitsDeckManager deckManager)
    {
        _newUnit = newUnit;
        _oldUnit = oldUnit;
        _deckManager = deckManager;
        GetComponent<Button>().onClick.AddListener(OnClick);
    }

    private void OnClick()
    {
        _deckManager.ReplaceUnitInDeck(_oldUnit, _newUnit);
        UnitDetailsPopupController.Instance.Close();
    }
}
