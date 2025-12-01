using System;
using System.Threading;
using UnityEngine;
using UnityEngine.UI;

public class CharacterStats : MonoBehaviour
{
    public int maxHealth = 100;
    public int currentHealth;
    public int damage;
    public float moveSpeed = 2f;
    public float attackRange = 1.2f;
    public float attackCooldown = 1.0f;
    public int spawnCount;

    public Team team;              // Character team
    public MonsterType monsterType;
    public bool lockedIn;

    private Animator animator;
    [SerializeField] Image _hpBar;
    private bool isDead = false;

    private void Awake()
    {
        currentHealth = maxHealth;
        animator = GetComponent<Animator>();
    }

    public void Init(Team currentTeam, MonsterType type)
    {
        monsterType = type;
        team = currentTeam;

        if (currentTeam == Team.EnemyTeam) //Enemy
        {
            // Flip sprite instead of rotating
            var sr = GetComponentInChildren<SpriteRenderer>();
            if (sr != null)
                sr.flipX = true;

            //Change hp bar color to red
            _hpBar.color = Color.red;
        }
    }

    public void TakeDamage(int damage)
    {
        if (isDead) return; // Ignore damage after death

        currentHealth -= damage;
        _hpBar.fillAmount = ((float)currentHealth / 100);
        if (currentHealth <= 0)
        {
            Die();
        }
    }

    void Die()
    {
        if (isDead) return;
        isDead = true;

        Debug.Log(gameObject.name + " died!");

        // Play death animation
        if (animator != null)
        {
            animator.SetTrigger("dying");
        }

        // Disable AI / attack scripts so the unit stops moving/attacking
        EnemyAI ai = GetComponent<EnemyAI>();
        if (ai != null) ai.enabled = false;

        RangerAttack rangerAttack = GetComponent<RangerAttack>();
        if (rangerAttack != null) rangerAttack.enabled = false;

        TankAttack tankAttack = GetComponent<TankAttack>();
        if (tankAttack != null) tankAttack.enabled = false;

        // Optional: stop physics movement
        Rigidbody2D rb = GetComponent<Rigidbody2D>();
        if (rb != null) rb.linearVelocity = Vector2.zero;

        // Destroy the game object after 2 seconds (so death animation can play)
        Destroy(gameObject, 2f);
    }


}
