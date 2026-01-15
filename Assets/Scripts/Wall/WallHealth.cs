using System;
using UnityEngine;

public class WallHealth : MonoBehaviour
{
    [Header("Wall Stats")]
    public int maxHealth = 500;
    public int currentHealth;

    public bool destroyed = false;
    public event Action<int, int> OnHealthChanged;


    private void Awake()
    {
        currentHealth = maxHealth;
        Notify();

    }

    public void TakeDamage(int amount)
    {
        if (destroyed) return;

        currentHealth -= amount;
        currentHealth = Mathf.Max(0, currentHealth);
        Notify();

        if (currentHealth <= 0)
            Die();
    }

    private void Die()
    {
        if (destroyed) return;
        destroyed = true;

        //Debug.Log("WALL DESTROYED – PLAYER LOSES");

        //if (BattleManagerInstance() != null)
        //    BattleManagerInstance().ForceLose();
    }

    private void Notify()
    {
        OnHealthChanged?.Invoke(currentHealth, maxHealth);
    }
}
