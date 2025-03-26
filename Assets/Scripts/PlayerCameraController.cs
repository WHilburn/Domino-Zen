using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public float moveSpeed = 75f;
    public float lookSpeed = 3f;
    public float collisionRadius = 0.5f;
    public float verticalSpeed = 50f;
    public float damping = 5f; // Controls gradual stopping

    private Vector3 moveDirection;
    private Vector3 velocity = Vector3.zero;
    private float targetVerticalVelocity = 0f; // Tracks vertical movement for smooth transitions

    private float currentRotationX = 0f;
    private float currentRotationY = 0f;

    void Start()
    {
        // Initialize rotation values from the camera's current rotation
        Vector3 initialEulerAngles = transform.eulerAngles;
        currentRotationX = initialEulerAngles.x;
        currentRotationY = initialEulerAngles.y;
    }

    void OnEnable()
    {
        Vector3 initialEulerAngles = transform.eulerAngles;
        currentRotationX = initialEulerAngles.x;
        currentRotationY = initialEulerAngles.y;
    }
    void Update()
    {
        float currentMoveSpeed = Input.GetKey(KeyCode.LeftShift) ? moveSpeed * 3f : moveSpeed;

        // Get input for horizontal movement
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");

        Vector3 forward = transform.forward;
        Vector3 right = transform.right;

        // Keep movement on XZ plane
        forward.y = 0f;
        right.y = 0f;
        forward.Normalize();
        right.Normalize();

        Vector3 inputMoveDirection = (forward * vertical + right * horizontal).normalized;

        // Smoothly damp movement over time
        moveDirection = Vector3.Lerp(moveDirection, inputMoveDirection, Time.deltaTime * damping);

        // Vertical movement (Q/E keys & mouse scroll wheel)
        if (Input.GetKey(KeyCode.Q)) targetVerticalVelocity = -verticalSpeed/20f;
        else if (Input.GetKey(KeyCode.E)) targetVerticalVelocity = verticalSpeed/20f;
        else targetVerticalVelocity = Mathf.Lerp(targetVerticalVelocity, 0f, Time.deltaTime * damping); // Gradual stop

        // Apply mouse scroll with smooth interpolation
        float scrollInput = Input.GetAxis("Mouse ScrollWheel") * verticalSpeed;
        targetVerticalVelocity += scrollInput;
        targetVerticalVelocity = Mathf.Lerp(targetVerticalVelocity, 0f, Time.deltaTime * damping);

        // Add vertical movement smoothly
        moveDirection += Vector3.up * targetVerticalVelocity;

        // Compute new position with collision handling
        Vector3 desiredPosition = transform.position + moveDirection * currentMoveSpeed * Time.deltaTime;
        transform.position = HandleCollisions(transform.position, desiredPosition);

        // Camera Rotation (RMB to look around)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentRotationX -= mouseY * lookSpeed;
            currentRotationY += mouseX * lookSpeed;
            currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);

            transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        }
    }

    Vector3 HandleCollisions(Vector3 currentPos, Vector3 targetPos)
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
