using UnityEngine;

public class FireMageAttack : MonoBehaviour, IAttackStrategy
{
    [Header("Meteor settings")]
    public GameObject meteorPrefab;
    public float spawnHeight = 5f;
    public float horizontalRandomOffset = 0.5f;

    private float lastAttackTime;
    private ICombatTarget currentTarget;
    private CharacterStats stats;
    private Animator animator;

    private void Awake()
    {
        animator = GetComponent<Animator>();
        stats = GetComponent<CharacterStats>();
    }

    public void Attack(ICombatTarget target)
    {
        if (target == null || stats == null || !target.IsAlive) return;

        if (Time.time - lastAttackTime >= stats.attackCooldown)
        {
            lastAttackTime = Time.time;
            currentTarget = target;
            animator?.SetTrigger("attack");
        }
    }

    // Animation Event
    public void SpawnMeteor()
    {
        if (currentTarget == null || !currentTarget.IsAlive) return;
        if (meteorPrefab == null || stats == null) return;

        Vector3 targetPos = currentTarget.TargetTransform.position;

        Vector3 spawnPos = targetPos + Vector3.up * spawnHeight;
        spawnPos.x += Random.Range(-horizontalRandomOffset, horizontalRandomOffset);

        GameObject meteor = Instantiate(meteorPrefab, spawnPos, Quaternion.identity);

        MeteorProjectile proj = meteor.GetComponent<MeteorProjectile>();
        if (proj != null)
        {
            proj.Init(currentTarget, stats.damage, stats);
        }
    }
}
