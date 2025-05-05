using Cinemachine;
using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    #region Fields and Properties
    public static PlayerCameraController Instance { get; private set; }
    public float moveSpeed = 75f;
    public float lookSpeed = 3f;
    public float collisionRadius = 0.5f;
    private Vector3 moveDirection;
    private float currentRotationX = 0f;
    private float currentRotationY = 0f;
    private static float offScreenHorizontalTolerance = 120f; // Tolerance for off-screen rotation
    public CinemachineBrain brain;
    public Camera activeCamera;
    public bool isCameraEnabled = true; // Flag to enable/disable camera controls
    private bool windowFocused = true; // Flag to check if the window is focused
    public CinemachineVirtualCamera tutorialCamera;
    private static float cameraFOV = 60f; // Default camera field of view
    #endregion

    #region Unity Lifecycle
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
        // Reset moveDirection to prevent unintended movement
        moveDirection = Vector3.zero;

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
        if (Input.GetMouseButton(1)) 
        {
            HandleCameraRotation();
        }
        else
        {
            // Stabilize rotation when the right mouse button is released
            StabilizeRotation();
        }

        // Check if a domino is being held and adjust the camera to keep it in frame
        if (PlayerDominoPlacement.heldDomino != null)
        {
            KeepHeldDominoInFrame();
        }
    }
    #endregion

    #region Camera Control Methods
    private void ToggleCameraControls(bool enable)
    {
        isCameraEnabled = enable; // Update the flag based on the event
    }

    public void setCameraFOV(float fov)
    {
        cameraFOV = fov;
        GetComponent<CinemachineVirtualCamera>().m_Lens.FieldOfView = fov; // Set the camera's field of view
        if (tutorialCamera != null) tutorialCamera.m_Lens.FieldOfView = fov; // Set the tutorial camera's field of view
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
        // Add vertical movement smoothly
        if (PlayerDominoPlacement.heldDomino != null) return; // Prevent vertical movement if a domino is held
        moveDirection += Vector3.up * Input.GetAxis("Up/Down") * 0.75f;
    }

    private void ApplyMovement(float currentMoveSpeed)
    {
        // Only apply movement if there is valid input
        if (moveDirection.sqrMagnitude > Mathf.Epsilon)
        {
            // Compute new position with collision handling
            Vector3 desiredPosition = transform.position + moveDirection * currentMoveSpeed * Time.deltaTime;
            transform.position = HandleCollisions(transform.position, desiredPosition);
        }
    }

    private void HandleCameraRotation()
    {
        float mouseX = Input.GetAxis("Mouse X");
        float mouseY = Input.GetAxis("Mouse Y");

        // Only apply rotation if there is actual input
        if (Mathf.Abs(mouseX) > Mathf.Epsilon || Mathf.Abs(mouseY) > Mathf.Epsilon)
        {
            currentRotationX -= mouseY * lookSpeed;
            currentRotationY += mouseX * lookSpeed;
            currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);

            transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        }
    }

    private void StabilizeRotation()
    {
        // Ensure no residual rotation occurs by clamping or resetting values
        currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);
        transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
    }

    private void KeepHeldDominoInFrame()
    {
        GameObject heldDomino = PlayerDominoPlacement.heldDomino;
        if (heldDomino == null || activeCamera == null) return;

        Vector3 screenPoint = activeCamera.WorldToScreenPoint(heldDomino.transform.position);
        bool isOutOfFrameHorizontally = screenPoint.x < -offScreenHorizontalTolerance || screenPoint.x > Screen.width + offScreenHorizontalTolerance;

        if (isOutOfFrameHorizontally)
        {
            Vector3 directionToDomino = (heldDomino.transform.position - transform.position).normalized;

            // Calculate the target Y-axis rotation to look at the domino
            float targetYRotation = Quaternion.LookRotation(directionToDomino).eulerAngles.y;

            // Smoothly interpolate the camera's Y-axis rotation towards the target rotation
            currentRotationY = Mathf.LerpAngle(currentRotationY, targetYRotation, lookSpeed * 0.25f * Time.deltaTime);
            transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        }
    }
    #endregion

    #region Collision Handling
    private Vector3 HandleCollisions(Vector3 currentPos, Vector3 targetPos)
    {
        Vector3 movementVector = targetPos - currentPos;
        float distance = movementVector.magnitude;

        // Avoid unnecessary calculations for very small movements
        if (distance < 0.001f) return currentPos;

        RaycastHit hit;
        if (Physics.CapsuleCast(currentPos, currentPos + Vector3.up * collisionRadius, collisionRadius, movementVector.normalized, out hit, distance))
        {
            Vector3 slideDirection = Vector3.ProjectOnPlane(movementVector, hit.normal);
            return currentPos + slideDirection; // Slide along surfaces
        }

        return targetPos;
    }
    #endregion
}