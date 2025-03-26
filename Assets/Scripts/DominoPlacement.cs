using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;
using Cinemachine;

public class DominoPlacement : MonoBehaviour
{
    public GameObject dominoPrefab;
    public GameObject handPrefab;
    private GameObject heldDomino;   // The domino currently being held
    private Rigidbody heldRb;        // Rigidbody of the held domino
    private Transform heldHand;
    private Vector3 anchor;
    private DecalProjector decalProjector;
    private float savedDrag;
    private float savedAngularDrag;
    public float followSpeed = 1f;  // How quickly the domino follows the mouse
    public float hoverOffset = 1.6f; // Distance the hand should hover over the ground when placing
    public float rotationSpeed = 100f; // Degrees per second to rotate dominoes
    public Camera activeCamera; // Reference to the active Cinemachine camera

    void Start()
    {
        activeCamera = FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        if (heldDomino)
        {
            MoveHeldDomino();
            HandleRotation();
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

        // Detect mouse click to pick up a domino
        if (Input.GetMouseButtonDown(0)) // Left Click
        {
            if (heldDomino == null)
            {
                TryPickUpDomino();
            }
        }

        // Delete the held domino when Esc is pressed
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeleteHeldDomino();
        }
    }

    void SpawnDomino()
    {
        if (!activeCamera.enabled || activeCamera == null) return;
        Vector3 spawnPos = GetMouseWorldPosition();
        Quaternion spawnRotation = Quaternion.Euler(90f, 0f, 0f); // Upright rotation

        heldDomino = Instantiate(dominoPrefab, spawnPos, spawnRotation);
        heldDomino.layer = LayerMask.NameToLayer("Ignore Raycast");
        heldDomino.GetComponent<Domino>().isHeld = true;
        decalProjector = heldDomino.GetComponent<DecalProjector>();
        decalProjector.enabled = true;
        heldRb = heldDomino.GetComponent<Rigidbody>();
        savedDrag = heldRb.drag;
        savedAngularDrag = heldRb.angularDrag;
        heldRb.drag = 10f;
        heldRb.angularDrag = 90f;
        heldRb.constraints = RigidbodyConstraints.FreezeRotationZ;

        // Create a hand
        GameObject handObject = Instantiate(handPrefab, spawnPos, Quaternion.identity);
        heldHand = handObject.transform;
        heldHand.position = spawnPos + new Vector3(0f, 0.5f, 0f); // Offset to top-middle

        // Move the domino to the hand position
        heldDomino.transform.position = heldHand.position;

        // Attach the domino to the hand using a SpringJoint
        SpringJoint spring = heldDomino.AddComponent<SpringJoint>();
        spring.connectedBody = handObject.GetComponent<Rigidbody>();
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

    void TryPickUpDomino()
    {
        if (!activeCamera.enabled || activeCamera == null) return;
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Domino domino = hit.collider.GetComponent<Domino>();
            if (domino != null)
            {
                PickUpDomino(domino.gameObject);
            }
        }
    }

    void PickUpDomino(GameObject domino)
    {
        heldDomino = domino;
        heldDomino.GetComponent<Domino>().isHeld = true;
        heldDomino.layer = LayerMask.NameToLayer("Ignore Raycast");
        heldRb = heldDomino.GetComponent<Rigidbody>();

        // Prevent picking up dominos that are actively falling
        if (heldRb.velocity.magnitude > 1f || heldRb.angularVelocity.magnitude > 1f)
        {
            heldDomino = null;
            heldRb = null;
            return;
        }

        decalProjector = heldDomino.GetComponent<DecalProjector>();
        if (decalProjector) decalProjector.enabled = true;

        savedDrag = heldRb.drag;
        savedAngularDrag = heldRb.angularDrag;
        heldRb.drag = 10f;
        heldRb.angularDrag = 90f;
        heldRb.constraints = RigidbodyConstraints.FreezeRotationZ;

        // Create a hand object
        Vector3 spawnPos = heldDomino.transform.position;
        GameObject handObject = Instantiate(handPrefab, spawnPos, Quaternion.identity);
        heldHand = handObject.transform;
        heldHand.position = spawnPos + new Vector3(0f, 0.5f, 0f);

        // Attach the domino to the hand
        heldDomino.transform.position = heldHand.position;
        SpringJoint spring = heldDomino.AddComponent<SpringJoint>();
        spring.connectedBody = handObject.GetComponent<Rigidbody>();
        spring.anchor = heldDomino.GetComponent<Domino>().holdPoint;
        anchor = spring.anchor;
        spring.autoConfigureConnectedAnchor = false;
        spring.connectedAnchor = Vector3.zero;
        spring.tolerance = 0.001f;
        spring.spring = 500f;
        spring.damper = 1f;
        spring.massScale = 1f;

        heldRb.velocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
    }

    void ReleaseDomino()
    {
        if (heldDomino == null) return;

        heldDomino.layer = LayerMask.NameToLayer("Default");
        heldDomino.GetComponent<Domino>().isHeld = false;

        SpringJoint spring = heldDomino.GetComponent<SpringJoint>();
        if (spring != null) Destroy(spring);

        heldRb.useGravity = true;
        if (decalProjector) decalProjector.enabled = false;
        heldRb.velocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
        heldRb.drag = savedDrag;
        heldRb.angularDrag = savedAngularDrag;
        heldRb.constraints = RigidbodyConstraints.None;

        if (heldHand != null) Destroy(heldHand.gameObject);

        heldDomino = null;
        heldRb = null;
        heldHand = null;
    }

    public void DeleteHeldDomino()
    {
        if (heldDomino == null) return;

        // Destroy the held domino and clean up references
        Destroy(heldDomino);
        if (heldHand != null) Destroy(heldHand.gameObject);

        heldDomino = null;
        heldRb = null;
        heldHand = null;
    }

    void MoveHeldDomino()
    {
        if (heldHand == null) return;
        Vector3 targetPosition = GetMouseWorldPosition();
        heldHand.position = Vector3.Lerp(heldHand.position, targetPosition, followSpeed * Time.deltaTime);
    }

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

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            return hit.point + Vector3.up * hoverOffset;
        }
        return ray.origin + ray.direction * 5f;
    }
}