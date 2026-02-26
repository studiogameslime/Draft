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
        if (contentRoot != null)
        {
            // Get the Image component from contentRoot, or add it if it doesn't exist yet
            Image partImage = contentRoot.GetComponent<Image>();
            if (partImage == null)
            {
                partImage = contentRoot.gameObject.AddComponent<Image>();
            }

            if (part != null && part.partSprite != null)
            {
                partImage.enabled = true;
                partImage.sprite = part.partSprite;
                partImage.preserveAspect = true;

                // Set alpha directly on the Image based on ownership
                Color c = Color.white;
                c.a = owned ? 1f : missingAlpha;
                partImage.color = c;
            }
            else
            {
                // Hide the image if no part is assigned
                partImage.enabled = false;
            }
        }

        // Set background color based on rarity
        if (background && part != null)
        {
            background.color = part.rarity switch
            {
                PartRarity.Common => StyleManager.instance.commonColor,
                PartRarity.Rare => StyleManager.instance.rareColor,
                PartRarity.Epic => StyleManager.instance.epicColor,
                _ => Color.white
            };
        }

        if (lockedOverlay) lockedOverlay.SetActive(false);
    }

    private void SetMissingVisual()
    {
        if (background) background.color = missingColor;
        if (lockedOverlay) lockedOverlay.SetActive(false);

        // Dim the part image if it exists
        if (contentRoot != null)
        {
            Image partImage = contentRoot.GetComponent<Image>();
            if (partImage != null && partImage.enabled)
            {
                Color c = partImage.color;
                c.a = missingAlpha;
                partImage.color = c;
            }
        }
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

}
