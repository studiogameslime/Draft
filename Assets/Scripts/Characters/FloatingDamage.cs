using UnityEngine;

public class FloatingDamage : MonoBehaviour
{
    public float floatingDamageDestroytime = 2f;
    public Vector3 offset = new Vector3(0, 2, 0);
    // Start is called once before the first execution of Update after the MonoBehaviour is created
    void Start()
    {
        Destroy(gameObject, floatingDamageDestroytime);
        transform.localPosition += offset;
    }

    // Update is called once per frame
    void Update()
    {
        
    }
}
