using UnityEngine;

public class CameraAnimation : MonoBehaviour
{
    public static CameraAnimation instance;
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        animator = GetComponent<Animator>();
    }

    public void EnterGridMode()
    {
        animator.SetTrigger("MoveToGrid");
    }

    public void EnterBattleMode()
    {
        animator.SetTrigger("MoveToBattle");
    }

    

}
