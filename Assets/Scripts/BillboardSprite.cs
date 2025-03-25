using UnityEngine;

public class BillboardSprite : MonoBehaviour
{
    private Camera mainCamera;

    void Start()
    {
        mainCamera = FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        // Make the hand face the camera
        transform.LookAt(transform.position + mainCamera.transform.forward);
    }
}
