using System;
using System.Collections;
using UnityEngine;

[RequireComponent(typeof(CharacterStats))]
[RequireComponent(typeof(UnitAI))]
public class NinjaStealthRun : MonoBehaviour
{
    [Header("Stealth Visual")]
    [SerializeField, Range(0f, 1f)] private float stealthAlpha = 0.5f;

    [Header("Dash Stretch (optional)")]
    [SerializeField] private bool useStretch = true;
    [SerializeField] private Vector3 dashStretchScale = new Vector3(1.2f, 0.85f, 1f);

    private CharacterStats stats;
    private UnitAI ai;
    private Animator animator;
    private NinjaGhostTrail ghostTrail;

    private bool didInitialStealthRun;
    private Vector3 originalScale;

    /// Fired when a stealth run finishes (after reaching target or target dies).
    public event Action OnStealthRunComplete;

    private void Awake()
    {
        stats = GetComponent<CharacterStats>();
        ai = GetComponent<UnitAI>();
        animator = GetComponent<Animator>();
        ghostTrail = GetComponent<NinjaGhostTrail>();
        originalScale = transform.localScale;
    }

    private void OnEnable()
    {
        didInitialStealthRun = false;
        transform.localScale = originalScale;
    }

    public void DoInitialStealthRun()
    {
        if (didInitialStealthRun) return;
        if (stats == null || stats.currentHealth <= 0) return;
        if (ai == null) return;

        ICombatTarget target = ai.CurrentCombatTarget;
        if (target == null || !target.IsAlive || target.TargetTransform == null) return;

        didInitialStealthRun = true;
        StartCoroutine(StealthRunToTargetRoutine(target));
    }

    /// Allows skills to reset the flag so DoInitialStealthRun can be called again.
    public void ResetStealthRun()
    {
        didInitialStealthRun = false;
    }

    private IEnumerator StealthRunToTargetRoutine(ICombatTarget target)
    {
        stats.isUntargetable = true;
        stats.SetAlpha(stealthAlpha);

        ai.enabled = false;

        animator?.SetBool("isMoving", true);

        if (useStretch)
            transform.localScale = dashStretchScale;

        if (ghostTrail != null)
            ghostTrail.StartTrail();

        while (target != null && target.IsAlive && stats.currentHealth > 0 && target.TargetTransform != null)
        {
            float distance = Vector3.Distance(transform.position, target.TargetTransform.position);
            if (distance <= stats.attackRange)
                break;

            float step = stats.moveSpeed * Time.deltaTime;
            transform.position = Vector3.MoveTowards(transform.position, target.TargetTransform.position, step);
            yield return null;
        }

        if (ghostTrail != null)
            ghostTrail.StopTrail();

        animator?.SetBool("isMoving", false);

        stats.isUntargetable = false;
        stats.SetAlpha(1f);

        if (useStretch)
            transform.localScale = originalScale;

        ai.enabled = true;

        OnStealthRunComplete?.Invoke();
    }
}
