using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class WallHPUI : MonoBehaviour
{
    public static WallHPUI instance;

    [SerializeField] private TMP_Text wallHPText;
    [SerializeField] private Image wallHPFill; // fillAmount 0-1

    private void Awake()
    {
        instance = this;
    }

    private void Start()
    {
        var wall = FindFirstObjectByType<WallHealth>();
        if (wall != null)
        {
            wall.OnHealthChanged += UpdateUI;
            UpdateUI(wall.currentHealth, wall.maxHealth);
        }
    }

    private void OnDestroy()
    {
        var wall = FindFirstObjectByType<WallHealth>();
        if (wall != null)
            wall.OnHealthChanged -= UpdateUI;
    }

    private void UpdateUI(int current, int max)
    {
        if (wallHPText != null)
            wallHPText.text = $"{current} / {max}";

        if (wallHPFill != null)
            wallHPFill.fillAmount = max > 0 ? (float)current / max : 0f;
    }
}
