using UnityEngine;

public class PlayerCameraController : MonoBehaviour
{
    public float moveSpeed = 5f;
    public float lookSpeed = 3f;
    public float minDistance = 2f;
    public float maxDistance = 10f;
    public float collisionRadius = 0.5f;

    private Vector3 moveDirection;
    private Vector3 velocity;

    private float currentRotationX = 0f; // For pitch control
    private float currentRotationY = 0f; // For yaw control

    void Update()
    {
        // Check for holding Shift to double the movement speed
        float currentMoveSpeed = (Input.GetKey(KeyCode.LeftShift)) ? moveSpeed * 2f : moveSpeed;

        // Camera Movement (WASD, relative to camera orientation)
        float horizontal = Input.GetAxis("Horizontal");
        float vertical = Input.GetAxis("Vertical");
        
        // Get the camera's forward and right directions
        Vector3 forward = transform.forward;
        Vector3 right = transform.right;
        
        // Normalize the vectors to prevent diagonal movement from being faster
        forward.y = 0f; // Keep the movement on the XZ plane
        right.y = 0f;

        forward.Normalize();
        right.Normalize();

        // Calculate the direction to move the camera based on WASD input and camera orientation
        moveDirection = (forward * vertical + right * horizontal).normalized;

        // Apply movement with collision check
        Vector3 desiredPosition = transform.position + moveDirection * currentMoveSpeed * Time.deltaTime;
        if (!CheckForCollision(desiredPosition))
        {
            transform.position = Vector3.SmoothDamp(transform.position, desiredPosition, ref velocity, 0.1f);
        }

        // Camera Rotation (RMB to look around)
        if (Input.GetMouseButton(1))
        {
            float mouseX = Input.GetAxis("Mouse X");
            float mouseY = Input.GetAxis("Mouse Y");

            currentRotationX -= mouseY * lookSpeed;
            currentRotationY += mouseX * lookSpeed;

            // Clamp the pitch to prevent upside-down rotation
            currentRotationX = Mathf.Clamp(currentRotationX, -80f, 80f);

            // Apply the rotations
            transform.rotation = Quaternion.Euler(currentRotationX, currentRotationY, 0);
        }
    }

    bool CheckForCollision(Vector3 targetPosition)
    {
        // Cast a sphere from the camera position to the target position
        Collider[] hitColliders = Physics.OverlapSphere(targetPosition, collisionRadius);
        foreach (var hitCollider in hitColliders)
        {
            if (hitCollider.gameObject.CompareTag("Wall"))
            {
                return true; // Collision detected, prevent movement
            }
        }
        return false;
    }
}
