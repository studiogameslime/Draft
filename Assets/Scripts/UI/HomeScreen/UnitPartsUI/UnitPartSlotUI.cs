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

    [Header("Visual")]
    [SerializeField] private Color ownedColor = Color.green;
    [SerializeField] private Color missingColor = Color.gray;
    [Range(0f, 1f)]
    [SerializeField] private float missingAlpha = 0.35f;

    private GameObject _currentInstance;

    public void SetOwned(PartDefinition part)
    {
        SetPartInternal(part, owned: true);
    }

    public void SetMissing(PartDefinition part)
    {
        SetPartInternal(part, owned: false);
    }

    private void SetPartInternal(PartDefinition part, bool owned)
    {
        ClearInstance();

        if (part != null && part.prefab != null && contentRoot != null)
        {
            _currentInstance = Instantiate(part.prefab, contentRoot);
            _currentInstance.transform.localPosition = Vector3.zero;
            _currentInstance.transform.localScale = Vector3.one;

            float a = owned ? 1f : missingAlpha;
            SetInstanceAlpha(_currentInstance, a);
        }
        if (background)
        {

            background.color = part.rarity switch
            {
                PartRarity.Common => StyleManager.instance.commonColor,
                PartRarity.Rare => StyleManager.instance.rareColor,
                PartRarity.Epic => StyleManager.instance.epicColor,
                _ => Color.white
            };
        }
            //background.color = owned ? ownedColor : missingColor;
        
        if (lockedOverlay) lockedOverlay.SetActive(false); // no lock anymore
    }

    private void SetMissingVisual()
    {
        if (background) background.color = missingColor;
        if (lockedOverlay) lockedOverlay.SetActive(false);
        if (_currentInstance != null)
            SetInstanceAlpha(_currentInstance, missingAlpha);
    }

    public void SetCounts(int greenCount, int rareCount, int epicCount)
    {
        if (greenPartsCountText)
        {
            bool show = greenCount > 0;
            greenPartsCountText.transform.parent.gameObject.SetActive(show);
            if (show) greenPartsCountText.text = "X" + greenCount;
        }

        if (rarePartsCountText)
        {
            bool show = rareCount > 0;
            rarePartsCountText.transform.parent.gameObject.SetActive(show);
            if (show) rarePartsCountText.text = "X" + rareCount;
        }

        if (epicPartsCountText)
        {
            bool show = epicCount > 0;
            epicPartsCountText.transform.parent.gameObject.SetActive(show);
            if (show) epicPartsCountText.text = "X" + epicCount;
        }
    }

    private void ClearInstance()
    {
        if (_currentInstance != null)
        {
            Destroy(_currentInstance);
            _currentInstance = null;
        }
    }

    private void SetInstanceAlpha(GameObject root, float a)
    {
        a = Mathf.Clamp01(a);

        var srs = root.GetComponentsInChildren<SpriteRenderer>(true);
        foreach (var sr in srs)
        {
            var c = sr.color; c.a = a; sr.color = c;
        }

        var imgs = root.GetComponentsInChildren<Image>(true);
        foreach (var img in imgs)
        {
            var c = img.color; c.a = a; img.color = c;
        }

        var tmps = root.GetComponentsInChildren<TMP_Text>(true);
        foreach (var t in tmps)
        {
            var c = t.color; c.a = a; t.color = c;
        }
    }
}
