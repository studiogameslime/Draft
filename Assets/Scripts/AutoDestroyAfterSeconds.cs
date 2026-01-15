using UnityEngine;

public class AutoDestroyAfterSeconds : MonoBehaviour
{
    [SerializeField] private float seconds = 1.5f;
    private void Start() => Destroy(gameObject, seconds);
}
