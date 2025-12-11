using UnityEngine;
using UnityEngine.UI;

public class UnitsGridController : MonoBehaviour
{
    public static UnitsGridController Instance;

    private GridLayoutGroup grid;

    [Header("Expanded layout")]
    [SerializeField] private float extraHeight = 120f; // how much height to add when expanded

    private UnitCardView _expandedCard;
    private int _expandedOriginalSiblingIndex;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        grid = GetComponent<GridLayoutGroup>();
    }

    // Called from UnitCardView when a collection card is clicked
    public void OnCardClicked(UnitCardView card)
    {
        // clicking the same card closes it
        if (_expandedCard == card)
        {
            CollapseCurrent();
            return;
        }

        // if some other card is open, close it first
        if (_expandedCard != null)
        {
            CollapseCurrent();
        }

        ExpandCard(card);
    }

    private void ExpandCard(UnitCardView card)
    {
        _expandedCard = card;
        _expandedOriginalSiblingIndex = card.transform.GetSiblingIndex();

        // disable grid so it will not try to re-layout children
        if (grid != null)
            grid.enabled = false;

        // draw card above its siblings
        card.transform.SetAsLastSibling();

        // increase height only – card will grow downward because pivot is top-center
        card.Expand(extraHeight);

        // show quick action buttons on the card
        card.ShowQuickActions(true);
    }

    public void CollapseCurrent()
    {
        if (_expandedCard == null)
            return;

        // restore size
        _expandedCard.Collapse();

        // restore original sibling index
        _expandedCard.transform.SetSiblingIndex(_expandedOriginalSiblingIndex);

        // hide quick action buttons
        _expandedCard.ShowQuickActions(false);

        _expandedCard = null;

        // enable grid again and rebuild layout
        if (grid != null)
        {
            grid.enabled = true;
            LayoutRebuilder.ForceRebuildLayoutImmediate((RectTransform)grid.transform);
        }
    }

    public void ForceCollapse()
    {
        CollapseCurrent();
    }
}
