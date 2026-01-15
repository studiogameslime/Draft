using UnityEngine;

[RequireComponent(typeof(Camera))]
public class AutoCameraSize : MonoBehaviour
{
    public float targetWidth = 9f;     // fixed world width
    public float maxHeight = 16f;    // maximum allowed world height

    private Camera cam;

    void Start()
    {
        cam = GetComponent<Camera>();
        UpdateSize();
    }

    void Update()
    {
        UpdateSize();
    }

    void UpdateSize()
    {
        float aspect = (float)Screen.width / Screen.height;

        float desiredHeight = targetWidth / aspect;
        float ortho = desiredHeight * 0.5f;
        float maxOrtho = maxHeight * 0.5f;

        cam.orthographicSize = Mathf.Min(ortho, maxOrtho);
    }
}
