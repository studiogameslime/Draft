using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class BossHealthBarUI : MonoBehaviour
{
    public static BossHealthBarUI Instance { get; private set; }

    [Header("UI")]
    [SerializeField] private GameObject root;
    [SerializeField] private Image hpFillImage;
    [SerializeField] private TMP_Text bossNameText;
    [SerializeField] private TMP_Text hpNumbersText;
    [SerializeField] private Image headWithEyes;


    private CharacterStats boss;

    private void Awake()
    {
        Instance = this;
        if (root != null) root.SetActive(false);
    }

    public void Show(CharacterStats bossStats)
    {
        if (bossStats == null) return;

        Unbind();
        boss = bossStats;

        if (root != null) root.SetActive(true);

        if (bossNameText != null)
            bossNameText.text = boss.definition != null ? boss.definition.displayName : "BOSS";

        if (headWithEyes != null) headWithEyes.sprite = boss.definition != null ? boss.definition.HeadWithEyes : null;

        boss.OnHealthChanged += OnBossHealthChanged;
        boss.OnDied += OnBossDied;

        OnBossHealthChanged(boss);
    }

    public void Hide()
    {
        Unbind();
        if (root != null) root.SetActive(false);
    }

    private void Unbind()
    {
        if (boss != null)
        {
            boss.OnHealthChanged -= OnBossHealthChanged;
            boss.OnDied -= OnBossDied;
            boss = null;
        }
    }

    private void OnBossHealthChanged(CharacterStats s)
    {
        if (s == null) return;

        int cur = Mathf.Max(0, s.currentHealth);
        int max = Mathf.Max(1, s.maxHealth);

        if (hpFillImage != null)
        {
            // 0..1
            hpFillImage.fillAmount = (float)cur / max;
        }

        if (hpNumbersText != null)
            hpNumbersText.text = $"{cur}/{max}";
    }

    private void OnBossDied(CharacterStats s)
    {
        Hide();
    }
}
