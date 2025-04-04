using UnityEngine;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PlayerDominoPlacement : MonoBehaviour
{
    public static PlayerDominoPlacement Instance { get; private set; }
    public GameObject dominoPrefab;
    public GameObject handSpritePrefab; // Reference to the hand sprite prefab
    public static GameObject heldDomino;
    private Rigidbody heldRb;
    private Transform handAnchor; // Empty GameObject for domino attachment
    private RectTransform handSpriteRect; // RectTransform of the hand sprite
    private Vector3 anchor;
    private DecalProjector decalProjector;
    private float savedDrag;
    private float savedAngularDrag;
    public float followSpeed = 1f;
    public float maxHandSpeed = 3f;
    public float hoverOffset = 1.6f;
    public float rotationSpeed = 100f;
    public Camera activeCamera;
    public GameObject lockSpritePrefab; // Reference to the lock sprite prefab
    public Canvas uiCanvas; // Reference to the UI canvas

    private Vector3 handMouseOffset; // Offset between hand and mouse cursor
    private float initialHandElevation; // Store the initial elevation of the hand anchor

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
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
                SpawnDomino();
            else
                ReleaseDomino();
        }

        if (Input.GetMouseButtonDown(0)) // Left Click
        {
            if (heldDomino == null)
                TryPickUpDomino();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeleteHeldDomino();
        }
    }

    void SpawnDomino()
    {
        if (!IsCameraActive()) return;

        Vector3 spawnPos = GetMouseWorldPosition();
        Quaternion spawnRotation = Quaternion.Euler(0f, 0f, 0f);

        heldDomino = Instantiate(dominoPrefab, spawnPos, spawnRotation);
        InitializeHeldDomino();

        CreateHandAnchor(spawnPos);
        initialHandElevation = handAnchor.position.y; // Store the initial elevation
        AttachDominoToAnchor();
        CreateHandSprite();

        // Calculate the offset between the hand and the mouse cursor
        handMouseOffset = handAnchor.position - GetMouseWorldPosition();
    }

    void TryPickUpDomino()
    {
        if (!IsCameraActive()) return;

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Domino domino = hit.collider.GetComponent<Domino>();
            if (domino != null && !domino.isHeld)
            {
                if (domino.locked)
                {
                    domino.AnimateDomino(Domino.DominoAnimation.Jiggle);
                    ShowLockSprite(hit.point); // Show the lock sprite at the hit point
                    return;
                }
                PickUpDomino(domino.gameObject);
            }
                
        }
    }

    void PickUpDomino(GameObject domino)
    {
        heldDomino = domino;
        InitializeHeldDomino();

        if (IsDominoFalling())
        {
            ClearHeldDomino();
            return;
        }

        Vector3 spawnPos = AdjustSpawnPosition(heldDomino.transform.position);
        CreateHandAnchor(spawnPos);
        initialHandElevation = handAnchor.position.y; // Store the initial elevation
        AttachDominoToAnchor();
        CreateHandSprite();

        // Calculate the offset between the hand and the mouse cursor
        handMouseOffset = handAnchor.position - GetMouseWorldPosition();
    }

    private void ResetDominoProperties()
    {
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
    }

    public void ReleaseDomino()
    {
        if (heldDomino == null) return;

        ResetDominoProperties();
        DestroyHand();

        ClearHeldDomino();
    }

    public void DeleteHeldDomino()
    {
        if (heldDomino == null) return;

        Destroy(heldDomino);
        DestroyHand();

        ClearHeldDomino();
    }

    void MoveHeldDomino() // Also moves the hand anchor and updates the hand sprite
    {
        if (handAnchor == null) return;

        // Get the target position adjusted by the offset
        Vector3 targetPosition = GetMouseWorldPosition() + handMouseOffset;
        Vector3 targetFlat = new Vector3(targetPosition.x, initialHandElevation, targetPosition.z); // Maintain initial elevation
        float step = Mathf.Min(maxHandSpeed * Time.deltaTime, Vector3.Distance(handAnchor.position, targetFlat));
        handAnchor.position = Vector3.MoveTowards(handAnchor.position, targetFlat, step);

        // Update the hand sprite position on the UI canvas
        if (handSpriteRect != null)
        {
            Vector3 screenPosition = activeCamera.WorldToScreenPoint(handAnchor.position);
            handSpriteRect.position = screenPosition;
        }
    }

    void HandleRotation()
    {
        if (heldRb == null) return; // Ensure the rigidbody exists

        if (Input.GetKey(KeyCode.Q))
            heldRb.AddTorque(Vector3.up * -rotationSpeed, ForceMode.Force);

        if (Input.GetKey(KeyCode.E))
            heldRb.AddTorque(Vector3.up * rotationSpeed, ForceMode.Force);
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int environmentLayerMask = LayerMask.GetMask("EnvironmentLayer");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
            return hit.point + Vector3.up * hoverOffset;
        return ray.origin + ray.direction * 5f;
    }

    // Helper Methods
    private bool IsCameraActive()
    {
        return activeCamera != null && activeCamera.enabled;
    }

    private void InitializeHeldDomino()
    {
        heldDomino.layer = LayerMask.NameToLayer("Ignore Raycast");
        heldDomino.GetComponent<Domino>().isHeld = true;
        decalProjector = heldDomino.GetComponent<DecalProjector>();
        if (decalProjector) decalProjector.enabled = true;

        heldRb = heldDomino.GetComponent<Rigidbody>();
        savedDrag = heldRb.drag;
        savedAngularDrag = heldRb.angularDrag;
        heldRb.drag = 10f;
        heldRb.angularDrag = 90f;
        // heldRb.constraints = RigidbodyConstraints.FreezeRotationY;
    }

    private bool IsDominoFalling()
    {
        return heldRb.velocity.magnitude > 1f || heldRb.angularVelocity.magnitude > 1f;
    }

    private void ClearHeldDomino()
    {
        heldDomino = null;
        heldRb = null;
        handAnchor = null;
    }

    private Vector3 AdjustSpawnPosition(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, 3f))
            position = hit.point;
        else
            position.y -= heldDomino.transform.localScale.y * 0.5f;

        position.y += hoverOffset;
        return position;
    }

    private void CreateHandAnchor(Vector3 spawnPos)
    {
        GameObject anchorObject = new GameObject("HandAnchor");
        handAnchor = anchorObject.transform;
        handAnchor.position = spawnPos + new Vector3(0f, 0.5f, 0f);
    }

    private void AttachDominoToAnchor()
    {
        heldDomino.transform.position = handAnchor.position;

        SpringJoint spring = heldDomino.AddComponent<SpringJoint>();
        spring.connectedBody = handAnchor.gameObject.AddComponent<Rigidbody>();
        spring.connectedBody.isKinematic = true; // Make the anchor kinematic
        spring.connectedBody.useGravity = false; // Disable gravity on the anchor
        spring.anchor = DominoLike.holdPoint;
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

    private void CreateHandSprite()
    {
        if (handSpritePrefab == null || uiCanvas == null) return;

        GameObject handSprite = Instantiate(handSpritePrefab, uiCanvas.transform);
        handSpriteRect = handSprite.GetComponent<RectTransform>();
        if (handSpriteRect != null)
        {
            Vector3 screenPosition = activeCamera.WorldToScreenPoint(handAnchor.position);
            handSpriteRect.position = screenPosition;
        }
    }

    private void DestroyHand()
    {
        if (handAnchor != null)
            Destroy(handAnchor.gameObject);

        if (handSpriteRect != null)
            Destroy(handSpriteRect.gameObject);
    }

    void ShowLockSprite(Vector3 position)
    {
        if (lockSpritePrefab == null || uiCanvas == null) return;

        // Instantiate the lock sprite as a UI element
        GameObject lockSprite = Instantiate(lockSpritePrefab, uiCanvas.transform);
        LockSpriteFollower follower = lockSprite.AddComponent<LockSpriteFollower>();
        follower.Initialize(activeCamera, position, 1f); // Pass camera, world position, and fade duration
    }
}