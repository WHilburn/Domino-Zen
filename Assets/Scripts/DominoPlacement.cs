using UnityEngine;

public class DominoPlacement : MonoBehaviour
{
    public GameObject dominoPrefab;  // Assign your domino prefab in the Inspector
    private GameObject heldDomino;   // The domino currently being held
    private Rigidbody heldRb;        // Rigidbody of the held domino
    private Transform heldHand;
    private Vector3 anchor;
    private float savedDrag;
    private float savedAngularDrag;
    public float followSpeed = 5f;  // How quickly the domino follows the mouse
    public float cameraSpeed = 10f; // Speed of camera movement
    public float hoverOffset = 1.6f; // Distance the hand should hover over the ground when placing
    public float rotationSpeed = 100f; // Degrees per second to rotate dominoes
    public bool debug = true;

    void Update()
    {
        if (heldDomino)
        {
            MoveHeldDomino();
            HandleRotation(); // Add rotation controls
        }

        if (debug && heldHand != null)
        {
            Debug.DrawLine(heldHand.position + Vector3.left * 0.5f, heldHand.position + Vector3.right * 0.5f, Color.red);
            Debug.DrawLine(heldHand.position + Vector3.up * 0.5f, heldHand.position + Vector3.down * 0.5f, Color.red);
            Debug.DrawLine(heldHand.position + Vector3.forward * 0.5f, heldHand.position + Vector3.back * 0.5f, Color.red);

            Debug.DrawLine(heldDomino.transform.position + anchor + Vector3.left * 0.5f, heldDomino.transform.position + anchor + Vector3.right * 0.5f, Color.blue);
            Debug.DrawLine(heldDomino.transform.position + anchor + Vector3.up * 0.5f, heldDomino.transform.position + anchor + Vector3.down * 0.5f, Color.blue);
            Debug.DrawLine(heldDomino.transform.position + anchor + Vector3.forward * 0.5f, heldDomino.transform.position + anchor + Vector3.back * 0.5f, Color.blue);
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
    }

    void SpawnDomino()
    {
        // Spawn the domino as before
        Vector3 spawnPos = GetMouseWorldPosition();
        Quaternion spawnRotation = Quaternion.Euler(90f, 0f, 0f); // Upright rotation

        heldDomino = Instantiate(dominoPrefab, spawnPos, spawnRotation);
        heldRb = heldDomino.GetComponent<Rigidbody>();
        savedDrag = heldRb.drag;
        savedAngularDrag = heldRb.angularDrag;
        heldRb.drag = 10f;
        heldRb.angularDrag = 90f;
        heldRb.constraints = RigidbodyConstraints.FreezeRotationZ;

        // Create an invisible hand (it’s a placeholder for movement)
        GameObject handObject = new GameObject("Hand");
        heldHand = handObject.transform;
        heldHand.position = spawnPos + new Vector3(0f, 0.5f, 0f); // Offset to top-middle

        // Add a Rigidbody to the hand for physics-based movement
        Rigidbody handRb = handObject.AddComponent<Rigidbody>();
        handRb.isKinematic = true; // Hand follows cursor directly

        // Move the domino to the hand position
        heldDomino.transform.position = heldHand.position;

        // Attach the domino to the hand using a SpringJoint
        SpringJoint spring = heldDomino.AddComponent<SpringJoint>();
        spring.connectedBody = handRb;
        // spring.anchor = new Vector3(0f, 0.5f, 0f); // Anchor at top-middle of domino
        spring.anchor = heldDomino.GetComponent<Domino>().holdPoint;
        anchor = spring.anchor;
        spring.autoConfigureConnectedAnchor = false;
        spring.connectedAnchor = Vector3.zero;
        spring.tolerance = 0.001f;

        // Spring joint parameters to keep the domino "suspended" smoothly
        spring.spring = 500f;  // Increase spring strength to hold it in place
        spring.damper = 1f;   // Moderate damping for smoothness
        spring.massScale = 1f;

        heldRb.velocity = Vector3.zero; // Stop any initial movement
        heldRb.angularVelocity = Vector3.zero; // Prevent any initial spin
    }

    void ReleaseDomino()
    {
        if (heldDomino == null) return; // No domino to release

        // Remove the SpringJoint
        SpringJoint spring = heldDomino.GetComponent<SpringJoint>();
        if (spring != null)
        {
            Destroy(spring);
        }

        // Reactivate gravity when released
        heldRb.useGravity = true;

        // Allow the domino to fall naturally now
        heldRb.velocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
        heldRb.drag = savedDrag;
        heldRb.angularDrag = savedAngularDrag;
        heldRb.constraints = RigidbodyConstraints.None;

        // Destroy the "hand" object
        if (heldHand != null)
        {
            Destroy(heldHand.gameObject);
        }

        // Clear the references for future use
        heldDomino = null;
        heldRb = null;
        heldHand = null;
    }


    void MoveHeldDomino()
    {
        if (heldHand == null) return; // Prevents errors if no domino is held

        Vector3 targetPosition = GetMouseWorldPosition();
        
        // Smoothly move the hand to the target position
        heldHand.position = Vector3.Lerp(heldHand.position, targetPosition, followSpeed * Time.deltaTime);
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

    // void MoveCamera()
    // {
    //     Vector3 forward = Camera.main.transform.forward;
    //     Vector3 right = Camera.main.transform.right;

    //     // Prevent vertical movement (remove Y component)
    //     forward.y = 0f;
    //     right.y = 0f;
        
    //     forward.Normalize();
    //     right.Normalize();

    //     Vector3 moveDirection = Vector3.zero;

    //     if (Input.GetKey(KeyCode.W))
    //         moveDirection += forward;
    //     if (Input.GetKey(KeyCode.S))
    //         moveDirection -= forward;
    //     if (Input.GetKey(KeyCode.A))
    //         moveDirection -= right;
    //     if (Input.GetKey(KeyCode.D))
    //         moveDirection += right;

    //     Camera.main.transform.position += moveDirection * cameraSpeed * Time.deltaTime;
    // }

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
