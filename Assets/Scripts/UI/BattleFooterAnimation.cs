using UnityEngine;

public class BattleFooterAnimation : MonoBehaviour
{
    public static BattleFooterAnimation instance;
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        animator = GetComponent<Animator>();
    }


    public bool IsAnimating =>
        animator != null && (animator.IsInTransition(0) ||
        animator.GetCurrentAnimatorStateInfo(0).normalizedTime < 1f);

    public void EnterGridMode()
    {
        animator.SetTrigger("moveUp");
    }

    public void EnterBattleMode()
    {
        animator.SetTrigger("moveDown");
    }



}
