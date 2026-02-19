using UnityEngine;
using UnityEngine.UI;

[RequireComponent(typeof(CharacterStats))]
public class MinerBehaviour : MonoBehaviour
{
    [Header("Mining Settings")]
    public float timeToMine = 15f;
    public int soulsPerMine = 1;

    private CharacterStats stats;
    private SpawnedUnitOwnerLink link;
    private Image cellFillImage;

    private bool isInitialized = false;
    private Color originalFillColor = Color.white;
    private float currentTimer = 0f;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        link = GetComponent<SpawnedUnitOwnerLink>();
    }

    private void Update()
    {
        // Do nothing if battle is not actively running
        if (BattleManager.instance == null || !BattleManager.instance.IsBattleRunning)
            return;

        // Do nothing if miner is dead
        if (stats.currentHealth <= 0)
            return;

        // Wait until the link is fully established with the spawner UI
        if (!isInitialized)
        {
            InitializeMiner();
            return;
        }

        // Advance timer
        currentTimer += Time.deltaTime;

        // Update the spawner's UI fill ring
        if (cellFillImage != null)
        {
            cellFillImage.fillAmount = currentTimer / timeToMine;
        }

        // Check if it's time to produce a soul
        if (currentTimer >= timeToMine)
        {
            ProduceSouls();
            currentTimer = 0f; // Reset timer for the next soul
        }
    }

    private void InitializeMiner()
    {
        // Ensure enemies completely ignore the miner
        stats.isUntargetable = true;

        if (link != null && link.owner != null)
        {
            // Grab the UI fill image from the DropAreaCell
            var cell = link.owner.GetComponentInParent<DropAreaCell>();
            if (cell != null && cell.fillImage != null)
            {
                cellFillImage = cell.fillImage;
                originalFillColor = cellFillImage.color;

                // Change the progress ring color to souls to indicate mining
                cellFillImage.color = Color.blue;
            }

            // Start the timer fresh
            currentTimer = 0f;
            isInitialized = true;
        }
    }

    private void ProduceSouls()
    {
        if (SoulOrbSpawner.instance != null)
        {
            // Spawn soul visual that will automatically fly to UI and add balance
            SoulOrbSpawner.instance.SpawnSoul(transform.position + Vector3.up * 0.5f, soulsPerMine);
        }
    }

    private void OnDestroy()
    {
        // Reset the UI styling when the unit is destroyed at the end of the round
        if (cellFillImage != null)
        {
            cellFillImage.fillAmount = 0f;
            cellFillImage.color = originalFillColor;
        }
    }
}