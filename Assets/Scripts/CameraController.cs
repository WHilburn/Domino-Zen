using UnityEngine;

public class CameraController : MonoBehaviour
{
    public float rotationSpeed = .1f;    // Sensitivity of camera rotation
    public float verticalLimit = 80f;     // Limit the vertical rotation (up/down)
    public float zoomSpeed = 10f;         // Speed of zooming in and out
    public float minFov = 20f;            // Minimum field of view for zooming
    public float maxFov = 60f;            // Maximum field of view for zooming
    public float moveSpeed = 5f;          // Speed of lateral camera movement

    private float xRotation = 0f;         // Track current vertical rotation
    private Vector2 lastMousePosition;    // To track mouse movement

    private Camera cameraComponent;       // Reference to the Camera component
    private float targetFov;              // Field of view to smoothly zoom

    void Start()
    {
        cameraComponent = Camera.main;    // Get the main camera
        targetFov = cameraComponent.fieldOfView;  // Initialize target FOV to current camera FOV
    }

    void Update()
    {
        HandleCameraRotation();
        HandleZoom();
        HandleCameraMovement();
    }

    private void HandleCameraRotation()
    {
        // Check if the left mouse button is held down
        if (Input.GetMouseButton(0)) // 0 is the left mouse button
        {
            // Calculate mouse movement
            Vector2 mouseDelta = (Vector2)Input.mousePosition - lastMousePosition;

            // Update the last mouse position for the next frame
            lastMousePosition = Input.mousePosition;

            // Rotate the camera around the Y-axis (horizontal rotation)
            transform.Rotate(Vector3.up, mouseDelta.x * rotationSpeed, Space.World);

            // Adjust the vertical rotation (clamped to avoid flipping)
            xRotation -= mouseDelta.y * rotationSpeed;
            xRotation = Mathf.Clamp(xRotation, -verticalLimit, verticalLimit);

            // Apply vertical rotation to the camera (pitch)
            cameraComponent.transform.localRotation = Quaternion.Euler(xRotation, 0f, 0f);
        }
        else
        {
            // Update the last mouse position only when the mouse button is held down
            lastMousePosition = Input.mousePosition;
        }
    }

    private void HandleZoom()
    {
        // Get mouse scroll wheel input for zooming
        float zoomInput = Input.GetAxis("Mouse ScrollWheel");

        // Adjust the field of view based on the zoom input
        targetFov -= zoomInput * zoomSpeed;
        targetFov = Mathf.Clamp(targetFov, minFov, maxFov);  // Clamp FOV within min and max limits

        // Smoothly interpolate between current FOV and target FOV for zoom effect
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov, Time.deltaTime * zoomSpeed);
    }

    private void HandleCameraMovement()
    {
        // WASD movement for camera (relative to camera's current facing direction)
        float horizontalMovement = Input.GetAxis("Horizontal");  // A/D or Left/Right
        float verticalMovement = Input.GetAxis("Vertical");      // W/S or Up/Down

        Vector3 forward = cameraComponent.transform.forward;
        Vector3 right = cameraComponent.transform.right;

        // We set y to 0 to prevent the camera from moving up or down
        forward.y = 0f;
        right.y = 0f;

        // Move the camera laterally (left/right, forward/backward)
        transform.position += forward * verticalMovement * moveSpeed * Time.deltaTime;
        transform.position += right * horizontalMovement * moveSpeed * Time.deltaTime;
    }
}
