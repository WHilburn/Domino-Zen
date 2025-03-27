using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public float moveSpeed = 75f;
    public float lookSpeed = 3f;
    public float collisionRadius = 0.5f;
    public float verticalSpeed = 2f;
    public float damping = 20f; // Controls gradual stopping

    private Vector3 moveDirection;
    private float targetVerticalVelocity = 0f; // Tracks vertical movement for smooth transitions

    private float currentRotationX = 0f;
    private float currentRotationY = 0f;

    void Start()
    {
        InitializeRotation();
    }

    void OnEnable()
    {
        InitializeRotation();
    }

    void Update()
    {
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