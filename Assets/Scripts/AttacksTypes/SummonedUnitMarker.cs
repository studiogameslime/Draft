using UnityEngine;

public class SummonedUnitMarker : MonoBehaviour
{
    private SkeletonSummoner owner;

    public void SetOwner(SkeletonSummoner summoner)
    {
        owner = summoner;
    }

    public SkeletonSummoner Owner => owner;
}
