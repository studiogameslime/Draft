using UnityEngine;

public enum ArrowElement
{
    None,
    Ice,
    Fire,
    Poison
}

public class ArrowEffect : MonoBehaviour
{
    [Header("Colors")]
    [SerializeField] private Color iceColor = Color.cyan;
    [SerializeField] private Color fireColor = Color.red;
    [SerializeField] private Color poisonColor = Color.green;

    [Header("Particle System")]
    [SerializeField] private ParticleSystem mainEffect;
    [SerializeField] private ParticleSystem trailEffect;

    public void Apply(ArrowElement element)
    {

        Color c = GetColor(element);

        ApplyColor(mainEffect, c, play: element != ArrowElement.None);
        ApplyColor(trailEffect, c, play: element != ArrowElement.None);
    }

    private Color GetColor(ArrowElement element)
    {
        return element switch
        {
            ArrowElement.Ice => iceColor,
            ArrowElement.Fire => fireColor,
            ArrowElement.Poison => poisonColor,
            _ => Color.white
        };
    }

    private void ApplyColor(ParticleSystem ps, Color c, bool play)
    {
        if (ps == null) return;
        var main = ps.main;
        main.startColor = c;
        if (play)
        {
            ps.Play(true);
        }
        else
        {
            ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
        }
    }

    public void StopAll()
    {
        if (mainEffect != null)
            mainEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);

        if (trailEffect != null)
            trailEffect.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear);
    }
}
