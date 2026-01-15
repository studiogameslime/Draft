using UnityEngine;
using System.Collections.Generic;
using System.Linq;

public class PartDropManager : MonoBehaviour
{
    public static PartDropManager Instance;

    [Header("Config")]
    [SerializeField] private UnlockedUnitsManager unlockedUnitsManager;
    [SerializeField] private UnitRarityWeights rarityWeights;
    [SerializeField] private PlayerPartsInventory partsInventory;

    [Header("Resources")]
    [SerializeField] private string partsResourcesPath = "Parts";

    [Header("Drop chance per win")]
    [Range(0f, 1f)]
    [SerializeField] private float dropChancePerWin = 1f;

    public List<PartDefinition> _allPartsCache;

    private void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }

        Instance = this;
        DontDestroyOnLoad(gameObject);
        LoadAllPartsFromResources();
    }

    private void LoadAllPartsFromResources()
    {
        PartDefinition[] loaded;
        if (string.IsNullOrEmpty(partsResourcesPath))
            loaded = Resources.LoadAll<PartDefinition>("");
        else
            loaded = Resources.LoadAll<PartDefinition>(partsResourcesPath);

        _allPartsCache = loaded.Where(p => p != null).ToList();
        Debug.Log($"PartDropManager: loaded {_allPartsCache.Count} parts from Resources.");
    }

    // =======================
    // OLD WIN-BASED REWARD
    // (you can keep or remove)
    // =======================
    public void GiveRewardAfterWin()
    {
        if (Random.value > dropChancePerWin)
        {
            Debug.Log("No part dropped this time.");
            return;
        }

        var part = GetRandomPartForDeckUnits(PartRarity.Green);
        if (part == null)
        {
            Debug.LogWarning("No valid part to drop (no candidates).");
            return;
        }

        partsInventory.AddPart(part, 1);
        Debug.Log($"Player got part: {part.id}");
    }

    // =======================
    // NEW API FOR CHESTS
    // =======================

    /// <summary>
    /// Picks a random part for current deck units with the requested rarity,
    /// weighted by the unit rarity.
    /// </summary>
    public PartDefinition GetRandomPartForDeckUnits(PartRarity rarity)
    {
        if (rarityWeights == null)
        {
            Debug.LogError("PartDropManager: missing rarityWeights");
            return null;
        }

        if (_allPartsCache == null || _allPartsCache.Count == 0)
        {
            Debug.LogWarning("PartDropManager: no parts in cache");
            return null;
        }

        var deckUnits = UnitsDeckManager.Instance._deckUnits;
        if (deckUnits == null || deckUnits.Count == 0)
        {
            Debug.LogWarning("PartDropManager: no deck units");
            return null;
        }

        // Filter by: belongs to a deck unit + matches requested part rarity
        var candidates = _allPartsCache
            .Where(p => p.unit != null &&
                        deckUnits.Contains(p.unit) &&
                        p.rarity == rarity)
            .ToList();

        if (candidates.Count == 0)
        {
            Debug.LogWarning($"PartDropManager: no candidate parts for deck units with rarity {rarity}");
            return null;
        }

        float totalWeight = 0f;
        float[] weights = new float[candidates.Count];

        for (int i = 0; i < candidates.Count; i++)
        {
            var part = candidates[i];
            float w = rarityWeights.GetWeight(part.unit.rarity);
            if (w < 0f) w = 0f;

            weights[i] = w;
            totalWeight += w;
        }

        if (totalWeight <= 0f)
        {
            Debug.LogWarning("PartDropManager: total weight <= 0");
            return null;
        }

        float roll = Random.value * totalWeight;
        float accum = 0f;

        for (int i = 0; i < candidates.Count; i++)
        {
            accum += weights[i];
            if (roll <= accum)
                return candidates[i];
        }

        return candidates[candidates.Count - 1];
    }

    /// <summary>
    /// Picks and adds a part to the inventory based on requested rarity.
    /// Returns the part that was actually granted (or null if none).
    /// </summary>
    public PartDefinition GiveRandomPartForDeckUnits(PartRarity rarity)
    {
        var part = GetRandomPartForDeckUnits(rarity);
        if (part == null)
        {
            Debug.LogWarning($"PartDropManager: could not find part for rarity {rarity}");
            return null;
        }

        partsInventory.AddPart(part, 1);
        Debug.Log($"Player got part: {part.id} (rarity {rarity})");
        return part;
    }

    // You can keep the old GetRandomPartForUnlockedUnits() if you still use it elsewhere.
}
