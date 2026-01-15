using UnityEngine;

public class CameraAnimation : MonoBehaviour
{
    public static CameraAnimation instance;
    public BattleManager battleManager;
    private Animator animator;
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Awake()
    {
        instance = this;
        animator = GetComponent<Animator>();
    }

    public void ShowDeckUI()
    {
        battleManager.ShowDeck();
    }

    public void HideDeckUI()
    {
        battleManager.HideDeck();
    }

    public void EnterGridMode()
    {
        animator.SetTrigger("MoveToGrid");
    }

    public void EnterBattleMode()
    {
        HideDeckUI();
        animator.SetTrigger("MoveToBattle");
    }

    

}
