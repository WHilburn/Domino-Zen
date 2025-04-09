using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;
using UnityEngine.UI;

public class PlayerDominoPlacement : MonoBehaviour
{
    public static PlayerDominoPlacement Instance { get; private set; }
    public GameObject dominoPrefab;
    public GameObject hand3DPrefab; // Reference to the 3D hand prefab
    public static GameObject heldDomino;
    private Rigidbody heldRb;
    private Transform handAnchor; // Empty GameObject for domino attachment
    private RectTransform handSpriteRect; // RectTransform of the hand sprite
    private GameObject hand3DInstance; // Instance of the 3D hand
    private DominoSoundManager soundManager; // Reference to the SoundManager
    private Vector3 anchor;
    private DecalProjector decalProjector;
    private float savedDrag;
    private float savedAngularDrag;
    public float followSpeed = 1f;
    public float maxHandSpeed = 3f;
    public float hoverOffset = 1.6f;
    public float rotationSpeed = 100f;
    public float maxDistance = 15f; // Maximum distance from the camera
    public Camera activeCamera;
    public GameObject lockSpritePrefab; // Reference to the lock sprite prefab
    public Canvas uiCanvas; // Reference to the UI canvas
    private Quaternion savedRotation = Quaternion.identity; // Store the final rotation of the 3D hand
    private Vector3 handMouseOffset; // Offset between hand and mouse cursor
    private float initialHandElevation; // Store the initial elevation of the hand anchor
    public static UnityEvent<Domino> OnDominoReleased = new();
    public static bool placementEnabled = true; // Flag to enable/disable placement controls
    public static bool flickEnabled = true;

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        activeCamera = FindFirstObjectByType<Camera>();
        soundManager = FindObjectOfType<DominoSoundManager>(); // Get reference to the SoundManager
        TutorialManager.OnTogglePlacementControls.AddListener(TogglePlacementControls); // Subscribe to the event
    }

    void Update()
    {
        if (heldDomino)
        {
            MoveHeldDomino();
            HandleRotation();
            heldDomino.GetComponent<Domino>().currentState = Domino.DominoState.Held; // Encure the state is held
        }

        if (Input.GetKeyDown(KeyCode.Space) && placementEnabled)
        {
            if (heldDomino == null)
                SpawnDomino();
            else
                ReleaseDomino();
        }

        if (Input.GetMouseButtonDown(0) && placementEnabled) // Left Click
        {
            if (heldDomino == null)
                TryPickUpDomino();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeleteHeldDomino();
        }

        if (Input.GetKeyDown(KeyCode.F) && flickEnabled)
        {
            TryFlickDomino();
        }
    }

    public void TogglePlacementControls(bool enable)
    {
        placementEnabled = enable;
        Debug.Log($"Placement controls toggled: {placementEnabled}");
        if (!placementEnabled) DestroyHand(); // Destroy the hand when controls are disabled
    }

    void SpawnDomino()
    {
        if (!IsCameraActive()) return;

        Vector3 spawnPos = GetMouseWorldPosition();

        // Prevent spawning if the position is further than 15 units from the camera
        if (Vector3.Distance(activeCamera.transform.position, spawnPos) > maxDistance) return;

        Quaternion spawnRotation = savedRotation;

        heldDomino = Instantiate(dominoPrefab, spawnPos, spawnRotation);
        heldDomino.name = $"{dominoPrefab.name} {InGameUI.dominoCount + 1}";
        InitializeHeldDomino();

        CreateHandAnchor(spawnPos);
        initialHandElevation = handAnchor.position.y; // Store the initial elevation
        AttachDominoToAnchor();

        Create3DHand(spawnPos);
        // Calculate the offset between the hand and the mouse cursor
        handMouseOffset = handAnchor.position - GetMouseWorldPosition();
    }

    void TryPickUpDomino()
    {
        if (!IsCameraActive()) return;

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Prevent picking up if the hit point is further than 15 units from the camera
            if (Vector3.Distance(activeCamera.transform.position, hit.point) > maxDistance) return;

            Domino domino = hit.collider.GetComponent<Domino>();
            if (domino != null && domino.currentState != Domino.DominoState.Held)
            {
                if (domino.locked)
                {
                    domino.AnimateDomino(Domino.DominoAnimation.Jiggle);
                    ShowLockSprite(hit.point); // Show the lock sprite at the hit point
                    soundManager?.PlayArbitrarySound(soundManager.dominoLockedSound, 1, 1, domino.transform.position);
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

        Create3DHand(spawnPos);

        // Calculate the offset between the hand and the mouse cursor
        handMouseOffset = handAnchor.position - GetMouseWorldPosition();
    }

    private void ResetDominoProperties()
    {
        heldDomino.layer = LayerMask.NameToLayer("Default");
        heldDomino.GetComponent<Domino>().currentState = Domino.DominoState.Moving;

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
        OnDominoReleased.Invoke(heldDomino.GetComponent<Domino>()); // Invoke the event

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

        // Clamp the target position to within 15 units of the camera
        if (Vector3.Distance(activeCamera.transform.position, targetPosition) > maxDistance)
        {
            targetPosition = activeCamera.transform.position + 
                             (targetPosition - activeCamera.transform.position).normalized * maxDistance;
        }

        Vector3 targetFlat = new(targetPosition.x, initialHandElevation, targetPosition.z); // Maintain initial elevation
        float step = Mathf.Min(maxHandSpeed * Time.deltaTime, Vector3.Distance(handAnchor.position, targetFlat));
        handAnchor.position = Vector3.MoveTowards(handAnchor.position, targetFlat, step);

        if (hand3DInstance != null)
        {
            hand3DInstance.transform.position = handAnchor.position;
            // Do not match the domino's rotation to the hand
        }
    }

    void HandleRotation()
    {
        if (heldRb == null) return; // Ensure the rigidbody exists

        if (hand3DInstance != null)
        {
            float rotationDelta = 0f;

            if (Input.GetKey(KeyCode.Q))
                rotationDelta = -rotationSpeed * Time.deltaTime * 10f;

            if (Input.GetKey(KeyCode.E))
                rotationDelta = rotationSpeed * Time.deltaTime * 10f;

            hand3DInstance.transform.Rotate(Vector3.up, rotationDelta, Space.World);

            // Apply torque to the domino to match its Y-axis rotation to the hand
            Quaternion targetRotation = Quaternion.Euler(0f, hand3DInstance.transform.eulerAngles.y, 0f);
            Quaternion currentRotation = Quaternion.Euler(0f, heldDomino.transform.eulerAngles.y, 0f);
            Quaternion deltaRotation = targetRotation * Quaternion.Inverse(currentRotation);

            // Calculate the angular difference in degrees
            float angleDifference = Mathf.DeltaAngle(currentRotation.eulerAngles.y, targetRotation.eulerAngles.y);

            // Scale the torque based on the angular difference
            float torqueStrength = Mathf.Clamp01(Mathf.Abs(angleDifference) / 15f); // Scale down as the angle difference decreases
            Vector3 torque = new(0f, angleDifference * torqueStrength, 0f);

            // Apply the torque to the rigidbody
            heldRb.AddTorque(torque * rotationSpeed, ForceMode.Force);
        }
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int environmentLayerMask = LayerMask.GetMask("EnvironmentLayer");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
        {
            Vector3 position = hit.point + Vector3.up * hoverOffset;

            // Prevent the hand from going further than 15 units from the camera
            if (Vector3.Distance(activeCamera.transform.position, position) > maxDistance)
            {
                position = activeCamera.transform.position + 
                           (position - activeCamera.transform.position).normalized * maxDistance;
            }

            return position;
        }
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
        heldDomino.GetComponent<Domino>().currentState = Domino.DominoState.Held;
        decalProjector = heldDomino.GetComponent<DecalProjector>();
        if (decalProjector) decalProjector.enabled = true;

        heldRb = heldDomino.GetComponent<Rigidbody>();
        savedDrag = heldRb.drag;
        savedAngularDrag = heldRb.angularDrag;
        heldRb.drag = 10f;
        heldRb.angularDrag = 90f;
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
        GameObject anchorObject = new("HandAnchor");
        handAnchor = anchorObject.transform;
        handAnchor.position = spawnPos + new Vector3(0f, 0.5f, 0f);
    }

    private void AttachDominoToAnchor()
    {
        heldDomino.transform.position = handAnchor.position;
        heldDomino.transform.rotation = savedRotation; // Match the rotation of the hand

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

    private void Create3DHand(Vector3 spawnPos)
    {
        if (hand3DPrefab == null) return;

        hand3DInstance = Instantiate(hand3DPrefab);
        hand3DInstance.transform.position = spawnPos;
        hand3DInstance.transform.rotation = savedRotation;
    }

    private void DestroyHand()
    {
        if (handAnchor != null)
            Destroy(handAnchor.gameObject);

        if (handSpriteRect != null)
            Destroy(handSpriteRect.gameObject);

        if (hand3DInstance != null)
        {
            // Store the final rotation of the 3D hand before destroying it
            savedRotation = hand3DInstance.transform.rotation;
            Destroy(hand3DInstance);
        }
    }

    void ShowLockSprite(Vector3 position)
    {
        if (lockSpritePrefab == null || uiCanvas == null) return;

        // Instantiate the lock sprite as a UI element
        GameObject lockSprite = Instantiate(lockSpritePrefab, uiCanvas.transform);
        LockSpriteFollower follower = lockSprite.AddComponent<LockSpriteFollower>();
        follower.Initialize(activeCamera, position, 1f); // Pass camera, world position, and fade duration
    }

    private void TryFlickDomino()
    {
        if (!IsCameraActive()) return;

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Domino domino = hit.collider.GetComponent<Domino>();
            if (domino != null && domino.currentState != Domino.DominoState.Held)
            {
                FlickDomino(domino);
            }
        }
    }

    private void FlickDomino(Domino domino)
    {
        Rigidbody rb = domino.GetComponent<Rigidbody>();
        if (rb)
        {
            rb.isKinematic = false;

            // Get the holdPoint position (top of the domino)
            Vector3 holdPoint = DominoLike.holdPoint;

            // Convert the local holdPoint to world space
            Vector3 worldHoldPoint = domino.transform.TransformPoint(holdPoint);

            // Calculate the force direction relative to the camera
            Vector3 cameraToDomino = domino.transform.position - activeCamera.transform.position;
            Vector3 forceDirection = Vector3.Dot(cameraToDomino, domino.transform.forward) > 0
                ? domino.transform.forward
                : -domino.transform.forward;

            // Apply the force at the holdPoint in the calculated direction
            rb.AddForceAtPosition(forceDirection * 1f, worldHoldPoint, ForceMode.Impulse);
        }
    }
}