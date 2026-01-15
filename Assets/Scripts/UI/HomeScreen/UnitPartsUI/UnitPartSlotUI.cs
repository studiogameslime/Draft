using UnityEngine;
using UnityEngine.UI;
using TMPro;

public class UnitPartSlotUI : MonoBehaviour
{
    [Header("Config")]
    public PartSlot slotType;

    [Header("UI")]
    [SerializeField] private Image background;
    [SerializeField] private Transform contentRoot;
    [SerializeField] private GameObject lockedOverlay;

    [Header("Counts UI")]
    [SerializeField] private TMP_Text greenPartsCountText;
    [SerializeField] private TMP_Text rarePartsCountText;  
    [SerializeField] private TMP_Text epicPartsCountText;  

    [Header("Colors")]
    [SerializeField] private Color lockedColor = Color.gray;
    [SerializeField] private Color ownedColor = Color.green;

    private GameObject _currentInstance;

    public void SetLocked()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }

        if (background) background.color = lockedColor;
        if (lockedOverlay) lockedOverlay.SetActive(true);

        if (greenPartsCountText) greenPartsCountText.transform.parent.gameObject.SetActive(false);
        if (rarePartsCountText) rarePartsCountText.transform.parent.gameObject.SetActive(false);
        if (epicPartsCountText) epicPartsCountText.transform.parent.gameObject.SetActive(false);
    }

    public void SetOwned(PartDefinition part)
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }

        if (part != null && part.prefab != null && contentRoot != null)
        {
            _currentInstance = Instantiate(part.prefab, contentRoot);
            _currentInstance.transform.localPosition = Vector3.zero;
            _currentInstance.transform.localScale = Vector3.one;
        }

        if (background) background.color = ownedColor;
        if (lockedOverlay) lockedOverlay.SetActive(false);
    }

    public void SetCounts(int greenCount, int rareCount, int epicCount)
    {
        if (greenPartsCountText)
        {
            if (greenCount > 0)
            {
                greenPartsCountText.transform.parent.gameObject.SetActive(true);
                greenPartsCountText.text = "X" + greenCount;
            }
            else
            {
                greenPartsCountText.transform.parent.gameObject.SetActive(false);
            }
        }

        if (rarePartsCountText)
        {
            if (rareCount > 0)
            {
                rarePartsCountText.transform.parent.gameObject.SetActive(true);
                rarePartsCountText.text = "X" + rareCount;
            }
            else
            {
                rarePartsCountText.transform.parent.gameObject.SetActive(false);
            }
        }

        if (epicPartsCountText)
        {
            if (epicCount > 0)
            {
                epicPartsCountText.transform.parent.gameObject.SetActive(true);
                epicPartsCountText.text = "X" + epicCount;
            }
            else
            {
                epicPartsCountText.transform.parent.gameObject.SetActive(false);
            }
        }
    }
}
