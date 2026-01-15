using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

public class ChapterPreviewUnitAI : MonoBehaviour
{
    [Header("Movement Settings")]
    public float moveSpeed = 150f;              // pixels per second (UI space)
    public float minIdleTime = 0.3f;
    public float maxIdleTime = 0.8f;

    [Header("Walk Area (RectTransforms on the canvas)")]
    public RectTransform bottomLeft;            // bottom-left corner of walkable area (anchoredPosition)
    public RectTransform topRight;             // top-right corner of walkable area

    [Header("Animation")]
    public Animator animator;                   // bool "IsMoving", trigger "Hit"

    [Header("Fight Settings")]
    public float collisionDistance = 40f;       // distance (pixels) to trigger a hit
    public float hitDuration = 0.3f;            // duration of hit animation
    public float postFightCooldown = 0.5f;      // time after hit before they can fight again
    public float sideMargin = 30f;              // small margin from center so they לא ייפגשו שוב על הקו האמצעי

    // --- Internal ---
    private RectTransform rect;
    private Image characterImage;

    private Vector2 targetPos;
    private bool hasTarget = false;
    private bool isFighting = false;
    private float idleTimer = 0f;
    private float fightCooldown = 0f;
    private bool isCelebrate = false;

    private static readonly List<ChapterPreviewUnitAI> allUnits = new();

    private void Awake()
    {
        rect = GetComponent<RectTransform>();
        characterImage = GetComponent<Image>();

        if (animator == null)
            animator = GetComponent<Animator>();
    }

    private void OnEnable()
    {
        allUnits.Add(this);
        hasTarget = false;
        idleTimer = 0f;
        isFighting = false;
        fightCooldown = 0f;
        SetMoving(false);
    }

    private void OnDisable()
    {
        allUnits.Remove(this);
    }

    private void Update()
    {
        if (isFighting || isCelebrate)
            return;

        // cooldown after fight
        if (fightCooldown > 0f)
            fightCooldown -= Time.deltaTime;

        // small idle when reaching a target
        if (idleTimer > 0f)
        {
            idleTimer -= Time.deltaTime;
            if (idleTimer <= 0f)
            {
                hasTarget = false; // pick new random target on next frame
            }
            return;
        }

        // pick a random target if needed
        if (!hasTarget)
        {
            PickRandomTarget();
            hasTarget = true;
        }

        MoveTowardsTarget();
        CheckForFight();
    }

    // ================= MOVEMENT =================

    private void PickRandomTarget()
    {
        if (bottomLeft == null || topRight == null)
        {
            targetPos = rect.anchoredPosition;
            return;
        }

        float minX = bottomLeft.anchoredPosition.x;
        float maxX = topRight.anchoredPosition.x;
        float minY = bottomLeft.anchoredPosition.y;
        float maxY = topRight.anchoredPosition.y;

        float x = Random.Range(minX, maxX);
        float y = Random.Range(minY, maxY);

        targetPos = new Vector2(x, y);

        UpdateFlipTowards(targetPos);
        SetMoving(true);
    }

    private void MoveTowardsTarget()
    {
        Vector2 current = rect.anchoredPosition;
        Vector2 toTarget = targetPos - current;

        if (toTarget.sqrMagnitude < 0.1f)
        {
            // reached target  short idle
            idleTimer = Random.Range(minIdleTime, maxIdleTime);
            SetMoving(false);
            return;
        }

        Vector2 dir = toTarget.normalized;
        float step = moveSpeed * Time.deltaTime;
        rect.anchoredPosition = current + dir * step;
    }

    private Vector2 ClampToArea(Vector2 pos)
    {
        if (bottomLeft == null || topRight == null)
            return pos;

        float minX = bottomLeft.anchoredPosition.x;
        float maxX = topRight.anchoredPosition.x;
        float minY = bottomLeft.anchoredPosition.y;
        float maxY = topRight.anchoredPosition.y;

        float x = Mathf.Clamp(pos.x, minX, maxX);
        float y = Mathf.Clamp(pos.y, minY, maxY);
        return new Vector2(x, y);
    }

    private void UpdateFlipTowards(Vector2 target)
    {
        if (characterImage == null) return;

        if (target.x < rect.anchoredPosition.x)
            characterImage.rectTransform.localScale = new Vector3(-1f, 1f, 1f); // face left
        else
            characterImage.rectTransform.localScale = new Vector3(1f, 1f, 1f);  // face right
    }

    // ================= FIGHT =================

    private void CheckForFight()
    {
        if (fightCooldown > 0f) return;

        foreach (var other in allUnits)
        {
            if (other == this) continue;
            if (other.isFighting) continue;
            if (other.fightCooldown > 0f) continue;

            float dist = Vector2.Distance(rect.anchoredPosition, other.rect.anchoredPosition);
            if (dist <= collisionDistance)
            {
                // only one of them starts the fight coroutine
                if (GetInstanceID() < other.GetInstanceID())
                {
                    StartCoroutine(FightRoutine(other));
                }
                break;
            }
        }
    }

    private IEnumerator FightRoutine(ChapterPreviewUnitAI other)
    {
        isFighting = true;
        other.isFighting = true;

        // stop movement for both
        SetMoving(false);
        other.SetMoving(false);
        hasTarget = false;
        other.hasTarget = false;

        // play hit animation
        animator?.SetTrigger("Hit");
        other.animator?.SetTrigger("Hit");

        yield return new WaitForSeconds(hitDuration);

        animator?.ResetTrigger("Hit");
        other.animator?.ResetTrigger("Hit");

        // read positions now
        Vector2 posA = rect.anchoredPosition;
        Vector2 posB = other.rect.anchoredPosition;

        // decide who is left/right now
        ChapterPreviewUnitAI leftUnit, rightUnit;
        if (posA.x <= posB.x)
        {
            leftUnit = this;
            rightUnit = other;
        }
        else
        {
            leftUnit = other;
            rightUnit = this;
        }

        // ================== KEY PART: FORCE OPPOSITE SIDES ==================

        // global walk area X range
        float minX = bottomLeft.anchoredPosition.x;
        float maxX = topRight.anchoredPosition.x;
        float midX = (minX + maxX) * 0.5f;

        float leftMax = midX - sideMargin;   // left side goes only עד פה
        float rightMin = midX + sideMargin;  // right side only מכאן ומעלה

        // clamp for safety (אם המפה קטנה מאוד)
        if (leftMax < minX) leftMax = minX;
        if (rightMin > maxX) rightMin = maxX;

        Vector2 leftPos = leftUnit.rect.anchoredPosition;
        Vector2 rightPos = rightUnit.rect.anchoredPosition;

        // pick random X on **left side** and **right side**
        float leftTargetX = Random.Range(minX, leftMax);
        float rightTargetX = Random.Range(rightMin, maxX);

        Vector2 leftTarget = new Vector2(leftTargetX, leftPos.y);
        Vector2 rightTarget = new Vector2(rightTargetX, rightPos.y);

        // final clamp (מעטפת הגנה)
        leftTarget = leftUnit.ClampToArea(leftTarget);
        rightTarget = rightUnit.ClampToArea(rightTarget);

        // make sure left is REALLY left, right is REALLY right
        if (leftTarget.x >= rightTarget.x)
        {
            float center = (leftTarget.x + rightTarget.x) * 0.5f;
            float halfSep = Mathf.Abs(rightTargetX - leftTargetX) * 0.5f;

            leftTarget.x = center - halfSep;
            rightTarget.x = center + halfSep;
        }

        // assign new targets
        leftUnit.targetPos = leftTarget;
        rightUnit.targetPos = rightTarget;

        leftUnit.hasTarget = true;
        rightUnit.hasTarget = true;

        // flip towards directions
        leftUnit.UpdateFlipTowards(leftTarget);
        rightUnit.UpdateFlipTowards(rightTarget);

        // cooldown (as timer)
        fightCooldown = postFightCooldown;
        other.fightCooldown = postFightCooldown;

        // resume walking towards these new targets
        leftUnit.SetMoving(true);
        rightUnit.SetMoving(true);

        isFighting = false;
        other.isFighting = false;
    }

    // ================= ANIMATION HELPER =================

    private void SetMoving(bool moving)
    {
        if (animator == null) return;
        animator.SetBool("IsMoving", moving);
    }

    public void PlayWinning()
    {
        isCelebrate = true;
        isFighting = false;
        hasTarget = false;
        idleTimer = 0f;
        fightCooldown = 999f;

        SetMoving(false);

        if (animator != null)
        {
            animator.ResetTrigger("Hit");
            animator.SetBool("IsMoving", false);
            animator.SetTrigger("Winning"); 
        }
    }

}
