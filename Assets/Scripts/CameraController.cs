using UnityEngine;
using System.Collections.Generic;

public class CameraController : MonoBehaviour
{
    public float rotationSpeed = .1f;    // Sensitivity of camera rotation
    public float verticalLimit = 80f;     // Limit the vertical rotation (up/down)
    public float zoomSpeed = 10f;         // Speed of zooming in and out
    public float minFov = 40f;            // Minimum field of view for zooming
    public float maxFov = 60f;            // Maximum field of view for zooming
    public float moveSpeed = 5f;          // Speed of lateral camera movement
    private float speedMultiplier = 1f;   // Speed multiplier for camera movement

    private float xRotation = 0f;         // Track current vertical rotation
    private Vector2 lastMousePosition;    // To track mouse movement

    private Camera cameraComponent;       // Reference to the Camera component
    public List<Transform> fallingDominoes = new List<Transform>();
    public Bounds bounds;
    private bool trackingDominoes = false;
    public float trackingTransitionSpeed = 2f;  // Smooth transition speed
    public float trackingFOVSpeed = 2f;  // Smooth zoom speed
    public Vector3 targetPosition;
    public float targetFov;

    void Start()
    {
        cameraComponent = Camera.main;    // Get the main camera
    }

    void Update()
    {   
        if (fallingDominoes.Count < 2 && trackingDominoes)
        {
            trackingDominoes = false;
        }
        else if (fallingDominoes.Count >= 2 && !trackingDominoes)
        {
            trackingDominoes = true;
            var dominoPlacement = FindObjectOfType<DominoPlacement>();
        }
        if (trackingDominoes)
        {
            UpdateTrackingCamera();
        }
        else
        {
            if (Input.GetKeyDown(KeyCode.LeftShift)) // double camera speed
            {
                speedMultiplier *= 2;
            }
            if (Input.GetKeyUp(KeyCode.LeftShift)) // reset camera speed
            {
                speedMultiplier /= 2;
            }
            HandleCameraRotation();
            HandleZoom();
            HandleCameraMovement();
            cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, 60, Time.deltaTime * trackingFOVSpeed);
        }

    }

    public void TrackFallingDominoes(List<Transform> dominos)
    {
        fallingDominoes = dominos;
        trackingDominoes = true;
    }

    private void UpdateDominoBounds()
    {
        if (fallingDominoes.Count == 0) return;

        // Compute bounding box
        bounds = new Bounds(fallingDominoes[0].position, Vector3.zero);
        foreach (Transform domino in fallingDominoes)
        {
            bounds.Encapsulate(domino.position);
        }

        // Set target camera position
        Vector3 center = bounds.center;
        float maxExtent = bounds.extents.magnitude;

        // Adjust the target position to ensure all dominoes are in view
        targetPosition = center + new Vector3(0, maxExtent * 2, -maxExtent * 2);
        targetFov = Mathf.Clamp(maxExtent * 10, minFov, maxFov);
    }

    private void UpdateTrackingCamera()
    {
        if (!trackingDominoes) return;

        UpdateDominoBounds();

        // Smoothly move camera
        transform.position = Vector3.Lerp(transform.position, targetPosition, Time.deltaTime * trackingTransitionSpeed);

        // Smoothly adjust FOV
        cameraComponent.fieldOfView = Mathf.Lerp(cameraComponent.fieldOfView, targetFov, Time.deltaTime * trackingFOVSpeed);
    }

    private void HandleCameraRotation()
    {
        // Check if the right mouse button is held down
        if (Input.GetMouseButton(1))  // Right mouse button
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
        // Get mouse scroll wheel input for moving the camera forward/backward
        float zoomInput = Input.GetAxis("Mouse ScrollWheel");

        // Move the camera forward/backward based on the zoom input
        Vector3 forwardMovement = cameraComponent.transform.forward * zoomInput * zoomSpeed;

        // Apply the movement to the camera's position
        transform.position += forwardMovement * speedMultiplier;
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
        transform.position += forward * verticalMovement * moveSpeed * Time.deltaTime * speedMultiplier;
        transform.position += right * horizontalMovement * moveSpeed * Time.deltaTime * speedMultiplier;
    }
}
