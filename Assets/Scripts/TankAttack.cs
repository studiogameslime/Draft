using UnityEngine;

public class TankAttack : MonoBehaviour, IAttackStrategy
{
    public int damage = 10;

    public void Attack(CharacterStats target)
    {
        GetComponent<Animator>().SetTrigger("attack");
        if (target != null)
            target.TakeDamage(damage);
    }
}
