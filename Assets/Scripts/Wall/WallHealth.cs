using System;
using System.Collections;
using UnityEngine;

public class WallHealth : MonoBehaviour
{
    [Header("Wall Stats")]
    public int maxHealth = 500;
    [SerializeField] private AudioClip hitSound;
    public int currentHealth;

    public bool destroyed = false;
    public event Action<int, int> OnHealthChanged;


    private void Awake()
    {
        if (MasteryBonusManager.Instance != null)
        {
            float bonus = MasteryBonusManager.Instance.GetPercent(MasteryStat.WallMaxHpPercent);
            maxHealth = Mathf.RoundToInt(maxHealth * (1f + bonus));
        }

        currentHealth = maxHealth;
        Notify();
    }

    public void TakeDamage(int amount)
    {
        if (destroyed) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        SoundManager.Instance?.PlaySFX(hitSound);
        Notify();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (destroyed) return;
        destroyed = true;

        //Debug.Log("WALL DESTROYED � PLAYER LOSES");

        //if (BattleManagerInstance() != null)
        //    BattleManagerInstance().ForceLose();
    }

    public void HealPercent(float percent, float duration = 1.5f)
    {
        if (destroyed || percent <= 0f) return;

        int healAmount = Mathf.RoundToInt(maxHealth * percent);
        int targetHealth = Mathf.Min(currentHealth + healAmount, maxHealth);
        StartCoroutine(HealOverTime(targetHealth, duration));
    }

    private IEnumerator HealOverTime(int targetHealth, float duration)
    {
        float startHealth = currentHealth;
        float elapsed = 0f;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float t = Mathf.Clamp01(elapsed / duration);
            float smoothT = t * t * (3f - 2f * t); // smoothstep
            currentHealth = Mathf.RoundToInt(Mathf.Lerp(startHealth, targetHealth, smoothT));
            Notify();
            yield return null;
        }

        currentHealth = targetHealth;
        Notify();
    }

    private void Notify()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
