using UnityEngine;
using Cinemachine;

public class FreeLookCameraController : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera;
    public float moveSpeed = 10f;  // Base movement speed
    public float speedMultiplier = 2f; // Speed multiplier when scrolling
    public float rotationSpeed = 5f; // Mouse look sensitivity

    private bool isMouseLookActive = false; // Track if RMB is held
    private float currentSpeed; // Adjusted movement speed

    void Start()
    {
        Cursor.lockState = CursorLockMode.None; // Don't lock cursor initially
        Cursor.visible = true;
        currentSpeed = moveSpeed;
    }

    void Update()
    {
        HandleMouseLook();
        HandleMovement();
        AdjustSpeed();
    }

    private void HandleMouseLook()
    {
        if (Input.GetMouseButtonDown(1)) // Right Mouse Button pressed
        {
            isMouseLookActive = true;
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }
        if (Input.GetMouseButtonUp(1)) // Right Mouse Button released
        {
            isMouseLookActive = false;
            Cursor.lockState = CursorLockMode.None;
            Cursor.visible = true;
        }

        if (isMouseLookActive)
        {
            float mouseX = Input.GetAxis("Mouse X") * rotationSpeed;
            float mouseY = Input.GetAxis("Mouse Y") * rotationSpeed;

            freeLookCamera.m_XAxis.Value += mouseX;
            freeLookCamera.m_YAxis.Value = Mathf.Clamp(freeLookCamera.m_YAxis.Value + (mouseY * -0.01f), 0f, 1f);
        }
    }


    private void HandleMovement()
    {
        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W)) moveDirection += transform.forward; // Forward
        if (Input.GetKey(KeyCode.S)) moveDirection -= transform.forward; // Backward
        if (Input.GetKey(KeyCode.A)) moveDirection -= transform.right;   // Left
        if (Input.GetKey(KeyCode.D)) moveDirection += transform.right;   // Right
        if (Input.GetKey(KeyCode.Q)) moveDirection -= transform.up;      // Down
        if (Input.GetKey(KeyCode.E)) moveDirection += transform.up;      // Up

        // Apply movement
        transform.position += moveDirection.normalized * currentSpeed * Time.deltaTime;
    }

    private void AdjustSpeed()
    {
        float scroll = Input.GetAxis("Mouse ScrollWheel");
        if (scroll > 0) currentSpeed *= speedMultiplier;
        if (scroll < 0) currentSpeed /= speedMultiplier;

        // Clamp speed to avoid excessive zooming
        currentSpeed = Mathf.Clamp(currentSpeed, 1f, 50f);
    }
}
