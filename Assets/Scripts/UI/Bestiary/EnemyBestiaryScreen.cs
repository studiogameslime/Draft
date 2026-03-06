using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class EnemyBestiaryScreen : MonoBehaviour
{
    [Header("Grid")]
    [SerializeField] private EnemyCardView cardPrefab;
    [SerializeField] private Transform gridContent;

    [Header("Popup Animation")]
    [SerializeField] private PopupAnimator popupAnimator;
    [SerializeField] private GameObject root;

    [Header("Details Popup")]
    [SerializeField] private EnemyDetailsPopupController detailsPopup;

    private readonly List<EnemyCardView> _cards = new();
    private List<EnemyEntry> _sortedEnemies;

    private struct EnemyEntry
    {
        public UnitDefinition def;
        public int world;
    }

    private void Awake()
    {
        _sortedEnemies = BuildSortedEnemyList();
    }

    public void OpenFromButton(RectTransform buttonRect)
    {
        if (popupAnimator == null)
            return;

        BuildGrid();
        root.SetActive(true);
        popupAnimator.OpenFromRect(buttonRect);
    }

    public void Close()
    {
        if (popupAnimator == null)
            return;

        popupAnimator.Close();
    }

    private static List<EnemyEntry> BuildSortedEnemyList()
    {
        var allLevels = Resources.LoadAll<LevelDefinition>("Levels");
        var enemyWorld = new Dictionary<string, EnemyEntry>();

        foreach (var level in allLevels)
        {
            if (level.rounds == null) continue;

            // Parse world number from level name (e.g. "1-3" → 1)
            int world = 0;
            string levelName = level.name;
            int dashIdx = levelName.IndexOf('-');
            if (dashIdx > 0)
                int.TryParse(levelName.Substring(0, dashIdx), out world);

            foreach (var round in level.rounds)
            {
                if (round.enemyPhases == null) continue;
                foreach (var phase in round.enemyPhases)
                {
                    if (phase.unit != null && phase.unit.unitTeam == Team.EnemyTeam
                        && !string.IsNullOrEmpty(phase.unit.id)
                        && !enemyWorld.ContainsKey(phase.unit.id))
                    {
                        enemyWorld[phase.unit.id] = new EnemyEntry
                        {
                            def = phase.unit,
                            world = world
                        };
                    }
                }
            }
        }

        return enemyWorld.Values
            .OrderBy(e => e.world)
            .ThenBy(e => e.def.isBoss ? 1 : 0)
            .ThenBy(e => e.def.displayName)
            .ToList();
    }

    private void BuildGrid()
    {
        foreach (var card in _cards)
        {
            if (card != null)
                Destroy(card.gameObject);
        }
        _cards.Clear();

        if (_sortedEnemies == null || _sortedEnemies.Count == 0)
            return;

        var save = GameData.Instance.Save;

        foreach (var entry in _sortedEnemies)
        {
            bool discovered = save.discoveredEnemyIds.Contains(entry.def.id);
            int kills = GetKillCount(save, entry.def.id);

            var card = Instantiate(cardPrefab, gridContent);
            card.Setup(entry.def, discovered, kills, this);
            _cards.Add(card);
        }
    }

    public void OnCardClicked(EnemyCardView card)
    {
        if (card.Definition == null || !card.IsDiscovered)
            return;

        if (detailsPopup != null)
            detailsPopup.Open(card.Definition, card.RectTransform);
    }

    private int GetKillCount(PlayerSaveData save, string enemyId)
    {
        var stat = save.enemyKillStats.FirstOrDefault(s => s.enemyId == enemyId);
        return stat?.kills ?? 0;
    }
}
