using UnityEngine;

public class DominoPlacement : MonoBehaviour
{
    public GameObject dominoPrefab;  // Assign your domino prefab in the Inspector
    private GameObject heldDomino;   // The domino currently being held
    private Rigidbody heldRb;        // Rigidbody of the held domino
    public float followSpeed = 20f;  // How quickly the domino follows the mouse
    public float cameraSpeed = 10f; // Speed of camera movement
    public float hoverOffset = 1.2f; // Distance the hand should hover over the ground when placing
    public float rotationSpeed = 100f; // Degrees per second to rotate dominoes

    void SpawnDomino()
    {
        // Instantiate the domino with a 90-degree rotation on the X-axis
        Vector3 spawnPos = GetMouseWorldPosition();
        Quaternion spawnRotation = Quaternion.Euler(90f, 0f, 0f); // Rotates on X-axis
        heldDomino = Instantiate(dominoPrefab, spawnPos, spawnRotation);
        
        heldRb = heldDomino.GetComponent<Rigidbody>();

        // Enable physics with some drag for smooth movement
        heldRb.useGravity = false;
        heldRb.drag = 5f;
    }

    void Update()
    {
        if (heldDomino)
        {
            MoveHeldDomino();
            HandleRotation(); // Add rotation controls
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (heldDomino == null)
            {
                SpawnDomino();
            }
            else
            {
                ReleaseDomino();
            }
        }

        MoveCamera(); // Camera movement with WASD
    }

    void MoveHeldDomino()
    {
        Vector3 targetPosition = GetMouseWorldPosition();

        // Offset the domino so its top-middle aligns with the hand position
        Vector3 dominoOffset = new Vector3(0f, -0.5f, 0f); // Moves it down by half its height

        Vector3 newPosition = Vector3.Lerp(heldDomino.transform.position, targetPosition + dominoOffset, followSpeed * Time.deltaTime);
        heldRb.MovePosition(newPosition);
    }


    // Rotate held domino on the Y-axis using Q and E
    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
        {
            heldDomino.transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);
        }
        if (Input.GetKey(KeyCode.E))
        {
            heldDomino.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
        }
    }

    void ReleaseDomino()
    {
        // Release the domino by resetting drag and removing the reference
        heldRb.drag = 0f; // Reset drag so it behaves naturally
        heldRb.useGravity = true;
        heldDomino = null;
        heldRb = null;
    }

    void MoveCamera()
    {
        Vector3 forward = Camera.main.transform.forward;
        Vector3 right = Camera.main.transform.right;

        // Prevent vertical movement (remove Y component)
        forward.y = 0f;
        right.y = 0f;
        
        forward.Normalize();
        right.Normalize();

        Vector3 moveDirection = Vector3.zero;

        if (Input.GetKey(KeyCode.W))
            moveDirection += forward;
        if (Input.GetKey(KeyCode.S))
            moveDirection -= forward;
        if (Input.GetKey(KeyCode.A))
            moveDirection -= right;
        if (Input.GetKey(KeyCode.D))
            moveDirection += right;

        Camera.main.transform.position += moveDirection * cameraSpeed * Time.deltaTime;
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = Camera.main.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point + Vector3.up * hoverOffset; // Hover 1.2 units above the surface
        }
        return ray.origin + ray.direction * 5f; // Default depth if no hit
    }

}
