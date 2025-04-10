using Cinemachine;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public static PlayerCameraController Instance { get; private set; }
    public float moveSpeed = 75f;
    public float lookSpeed = 3f;
    public float collisionRadius = 0.5f;
    public float verticalSpeed = 2f;
    public float damping = 20f; // Controls gradual stopping
    private Vector3 moveDirection;
    private float targetVerticalVelocity = 0f; // Tracks vertical movement for smooth transitions
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    public CinemachineBrain brain;
    public bool isCameraEnabled = true; // Flag to enable/disable camera controls
    private bool windowFocused = true; // Flag to check if the window is focused

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        InitializeRotation();
        brain = FindObjectOfType<CinemachineBrain>();
        TutorialManager.OnToggleCameraControls.AddListener(ToggleCameraControls); // Subscribe to the event
    }

    private void ToggleCameraControls(bool enable)
    {
        isCameraEnabled = enable; // Update the flag based on the event
    }

    void OnEnable()
    {
        InitializeRotation();
        brain = FindObjectOfType<CinemachineBrain>();
    }
    private void OnApplicationFocus(bool hasFocus)
    {
        windowFocused = hasFocus; // Update the flag based on the application focus state
        if (hasFocus)
        {
            InitializeRotation();
            brain = FindObjectOfType<CinemachineBrain>();

            // Reset mouse input deltas to prevent snapping
            Input.ResetInputAxes();
        }
    }

    void Update()
    {
        // Check if the active virtual camera is null, a different camera, or blending
        if (!isCameraEnabled ||
            brain == null ||
            brain.ActiveVirtualCamera == null || 
            brain.ActiveVirtualCamera.VirtualCameraGameObject != gameObject || 
            brain.IsBlending ||
            !windowFocused) // Check if the window is focused
        {
            return;
        }

        float currentMoveSpeed = GetCurrentMoveSpeed();

        // Handle movement
        HandleHorizontalMovement(currentMoveSpeed);
        HandleVerticalMovement();
        ApplyMovement(currentMoveSpeed);

        // Handle camera rotation
        if (Input.GetMouseButton(1)) HandleCameraRotation();
    }

    public void InitializeRotation()
    {
        Vector3 initialEulerAngles = transform.eulerAngles;
        currentRotationX = initialEulerAngles.x;
        currentRotationY = initialEulerAngles.y;
    }

    private float GetCurrentMoveSpeed()
    {
        return Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 3f : moveSpeed;
    }

    private void HandleHorizontalMovement(float currentMoveSpeed)
    {
        // Get input for horizontal movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Keep movement on XZ plane
        forward.y = 0f;
        right.y = 0f;

        // Calculate the input movement direction
        moveDirection = (forward * vertical + right * horizontal);
    }

    private void HandleVerticalMovement()
    {
        if(PlayerDominoPlacement.heldDomino != null) return; // Prevent camera movement while holding a domino

        // Vertical movement (Q/E keys & mouse scroll wheel)
        if (Input.GetKey(KeyCode.Q)) targetVerticalVelocity = -verticalSpeed / 20f;
        else if (Input.GetKey(KeyCode.E)) targetVerticalVelocity = verticalSpeed / 20f;
        else targetVerticalVelocity = Mathf.Lerp(targetVerticalVelocity, 0f, Time.deltaTime * damping); // Gradual stop

        // Apply mouse scroll with smooth interpolation
        float scrollInput = Input.GetAxis("Mouse ScrollWheel") * verticalSpeed;
        targetVerticalVelocity += scrollInput;
        targetVerticalVelocity = Mathf.Lerp(targetVerticalVelocity, 0f, Time.deltaTime * damping);

        // Add vertical movement smoothly
        moveDirection += Vector3.up * targetVerticalVelocity;
    }

    private void ApplyMovement(float currentMoveSpeed)
    {
        // Compute new position with collision handling
        Vector3 desiredPosition = transform.position + moveDirection * currentMoveSpeed * Time.deltaTime;
        transform.position = HandleCollisions(transform.position, desiredPosition);
    }

    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        currentRotationX -= mouseY * lookSpeed;
        currentRotationY += mouseX * lookSpeed;
        currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);

        transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
    }

    private Vector3 HandleCollisions(Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 movementVector = targetPos - currentPos;
        float distance = movementVector.magnitude;

        if (distance < 0.001f) return currentPos;

        RaycastHit hit;
        if (Physics.CapsuleCast(currentPos, currentPos + Vector3.up * collisionRadius, collisionRadius, movementVector.normalized, out hit, distance))
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(movementVector, hit.normal);
            return currentPos + slideDirection; // Slide along surfaces
        }

        return targetPos;
    }
}