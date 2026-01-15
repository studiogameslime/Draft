using System.Collections;
using UnityEngine;

public class HitFlash : MonoBehaviour
{
    private Material mat;

    void Awake()
    {
        mat = GetComponent<SpriteRenderer>().material;
    }

    public IEnumerator FlashWhite()
    {
        mat.SetFloat("_FlashAmount", 0.8f);
        yield return new WaitForSeconds(0.1f);
        mat.SetFloat("_FlashAmount", 0f);
    }
}
