using DG.Tweening;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.Rendering.Universal;

public class PlayerDominoPlacement : MonoBehaviour
{
    #region Fields and Properties
    public static PlayerDominoPlacement Instance { get; private set; }
    public GameObject dominoPrefab;
    public GameObject hand3DPrefab; // Reference to the 3D hand prefab
    public static GameObject heldDomino;
    private Rigidbody heldRb;
    private Transform handAnchor; // Empty GameObject for domino attachment
    private GameObject hand3DInstance; // Instance of the 3D hand
    private DominoSoundManager soundManager; // Reference to the SoundManager
    private Vector3 anchor;
    private float savedDrag;
    private float savedAngularDrag;
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
    public static bool placementLimited = false; // Flag to limit placement to a specific area
    public Material glowOutlineMaterial; // Reference to the glow outline material
    public Material placementDecalMaterial; // Material for the hollow rectangle decal
    public Material placementDecalMaterialRed;
    public Material dashedOutlineMaterial;
    public GameObject cylinderPrefab; // Reference to the cylinder prefab for placement visualization
    public Vector3 decalSize = new Vector3(1f, 1f, 1f); // Size of the decal
    public Vector3 decalPivot = new Vector3(0f, 0f, -1f); // Pivot point of the decal
    private Domino obstruction;
    private PlacementDecalManager placementDecalManager;
    private GlowOutlineManager glowOutlineManager;
    private Tween springTween; // Store the tween reference
    public bool bucketModeEnabled = false; // Flag to enable/disable bucket mode
    private PlayerObjectMovement objectMovementManager;
    #endregion
    #region Unity Methods
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        soundManager = FindObjectOfType<DominoSoundManager>(); // Get reference to the SoundManager
        TutorialManager.OnTogglePlacementControls.AddListener(TogglePlacementControls); // Subscribe to the event

        placementDecalManager = new PlacementDecalManager(
            placementDecalMaterial,
            placementDecalMaterialRed,
            dashedOutlineMaterial,
            decalSize,
            decalPivot,
            maxDistance,
            activeCamera,
            savedRotation
        );

        glowOutlineManager = new GlowOutlineManager(glowOutlineMaterial, activeCamera);
        objectMovementManager = gameObject.AddComponent<PlayerObjectMovement>();
        objectMovementManager.Initialize(activeCamera);
    }

    void Update()
    {
        if (InGameUI.paused || 
            DominoResetManager.Instance != null && 
            DominoResetManager.Instance.currentState != DominoResetManager.ResetState.Idle)
        {
            DestroyHand(); // Destroy the hand if the game is paused or in a reset state
            placementDecalManager.UpdatePlacementDecal(false, heldDomino, savedRotation);
            glowOutlineManager.RemoveGlowOutline();
            return;
        }

        if (placementLimited && !IsMousePointingAtTutorialBook())
        {
            return; // Prevent actions if placement is limited and not pointing at the "Tutorial Book"
        }

        if (heldDomino)
        {
            MoveHeldDomino();
            HandleRotation();
            heldDomino.GetComponent<Domino>().currentState = Domino.DominoState.Held; // Ensure the state is held
        }

        obstruction = placementDecalManager.CheckForObstruction(heldDomino, savedRotation, hoverOffset); // Check for obstructions

        if (Input.GetKeyDown(KeyCode.Space) && placementEnabled)
        {
            if (heldDomino == null)
                SpawnDomino();
            else
                ReleaseDomino();
        }

        if (Input.GetMouseButtonDown(0) && heldDomino == null && placementEnabled) // Left Click
        {
            TryPickUpDomino(); // Allow picking up dominoes regardless of placementLimited
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeleteHeldDomino();
        }

        if (Input.GetButtonDown("Interact") && flickEnabled)
        {
            TryFlickDomino();
        }

        glowOutlineManager.HandleMouseHover(heldDomino);
        placementDecalManager.UpdatePlacementDecal(placementEnabled, heldDomino, savedRotation); // Update the placement decal position and visibility
        HandleRotation(); // Handle rotation even when no domino is held
    }
    #endregion
    #region Placement Controls

    public void TogglePlacementControls(bool enable)
    {
        placementEnabled = enable;
        if (!placementEnabled) DestroyHand(); // Destroy the hand when controls are disabled
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

    private bool IsCameraActive()
    {
        return activeCamera != null && activeCamera.enabled;
    }

    private bool IsMousePointingAtTutorialBook() //Used during tutorial
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int layerMask = LayerMask.GetMask("EnvironmentLayer"); // Ignore dominoes in the raycast
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, layerMask))
        {
            return hit.collider.gameObject.name == "Tutorial Book";
        }
        return false;
    }

    #endregion

    #region Spawn Domino
    void SpawnDomino()
    {
        if (!IsCameraActive() || (placementLimited && !IsMousePointingAtTutorialBook())) return;

        Bucket bucket = null;
        if (bucketModeEnabled)
        {
            bucket = IsMousePointingAtBucket();
            if (bucket == null) return; // Exit if not pointing at a bucket
        }

        Vector3 spawnPos = GetMouseWorldPosition();
        Quaternion spawnRotation = savedRotation;
        if (bucketModeEnabled)
        {
            spawnPos = bucket.spawnLocation.position; // Use the bucket's spawn location
            spawnRotation = bucket.spawnLocation.rotation; // Use the bucket's rotation
        }

        // Prevent spawning if the position is further than 15 units from the camera
        if (Vector3.Distance(activeCamera.transform.position, spawnPos) > maxDistance) return;

        // Check for collisions with existing dominoes
        if (obstruction != null && obstruction != heldDomino)
        {
            obstruction.AnimateDomino(Domino.DominoAnimation.Jiggle); // Play jiggle animation
            InGameUI.Instance.CreateFloatingWorldText("Obstructed", spawnPos, 1f, 1f, true);
            soundManager?.PlayArbitrarySound(soundManager.dominoObstructedSound, 1, 1, spawnPos);
            return; // Prevent spawning
        }

        heldDomino = Instantiate(dominoPrefab, spawnPos, spawnRotation);
        heldDomino.name = $"{dominoPrefab.name} {InGameUI.dominoCount + 1}";
        InitializeHeldDomino();

        CreateHandAnchor(spawnPos); // Create the hand anchor
        initialHandElevation = handAnchor.position.y; // Store the initial elevation
        AttachDominoToAnchor();

        Create3DHand(spawnPos);
        // Calculate the offset between the hand and the mouse cursor
        if (bucketModeEnabled)
        {
            handMouseOffset = handAnchor.position - bucket.spawnLocation.position; // Adjust offset for bucket mode
        }
        else
        {
            handMouseOffset = handAnchor.position - GetMouseWorldPosition(); // Adjust offset for normal mode
        }
    }

    private void InitializeHeldDomino()
    {
        glowOutlineManager.RemoveGlowOutline();

        heldDomino.layer = LayerMask.NameToLayer("Ignore Raycast");
        heldDomino.GetComponent<Domino>().currentState = Domino.DominoState.Held;
        heldDomino.GetComponent<DecalProjector>().enabled = true;

        heldRb = heldDomino.GetComponent<Rigidbody>();
        savedDrag = heldRb.drag;
        savedAngularDrag = heldRb.angularDrag;
        heldRb.drag = 10f;
        heldRb.angularDrag = 90f;
    }

    #endregion
    #region Pick Up Domino

    void TryPickUpDomino()
    {
        if (!IsCameraActive()) return;

        if (bucketModeEnabled && IsMousePointingAtBucket())
        {
            SpawnDomino(); // Spawn a domino if pointing at a bucket
            return; // Prevent picking up if pointing at a bucket
        }

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Prevent picking up if the hit point is further than 15 units from the camera
            if (Vector3.Distance(activeCamera.transform.position, hit.point) > maxDistance) return;

            Domino domino = hit.collider.GetComponent<Domino>();
            if (domino != null && domino.currentState != Domino.DominoState.Held && domino.currentState != Domino.DominoState.Animating)
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
        domino.GetComponent<Domino>().stablePositionSet = false; // Reset stable position set
        // Preserve the existing rotation of the domino
        savedRotation = heldDomino.transform.rotation;
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

    #endregion

    #region Release/Delete Domino
    private void ResetDominoProperties()
    {
        Domino heldDominoScript = heldDomino.GetComponent<Domino>();
        heldDomino.layer = LayerMask.NameToLayer("Default");
        if (heldDominoScript.currentState != Domino.DominoState.Animating)
        {
            heldDominoScript.currentState = Domino.DominoState.Moving;
        }
        heldDomino.GetComponent<DecalProjector>().enabled = false;

        SpringJoint spring = heldDomino.GetComponent<SpringJoint>();
        if (spring != null) Destroy(spring);

        heldRb.useGravity = true;
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

        heldDomino.GetComponent<Domino>().DespawnDomino();
        DestroyHand();

        ClearHeldDomino();
    }
    #endregion
    #region Domino Movement

    void MoveHeldDomino() // Also moves the hand anchor and updates the hand sprite
    {
        if (handAnchor == null) return;

        Vector3 bucketOffset = Vector3.zero; // Offset for the bucket mode
        if (bucketModeEnabled) //Move hand/domino up if over a bucket
        {
            Collider[] hitColliders = Physics.OverlapBox(
                heldDomino.transform.position, 
                new Vector3(0.3f, 5f, 0.3f), 
                Quaternion.identity
            );

            foreach (var collider in hitColliders)
            {
                if (collider.CompareTag("Bucket"))
                {
                    bucketOffset = new Vector3(0f, 1.25f, 0f); // Adjust the offset for bucket mode
                    break;
                }
            }
        }

        // Get the target position adjusted by the offset
        Vector3 targetPosition = GetMouseWorldPosition() + handMouseOffset + bucketOffset;

        // Clamp the target position to within maxDistance units of the camera
        if (Vector3.Distance(activeCamera.transform.position, targetPosition) > maxDistance)
        {
            targetPosition = activeCamera.transform.position + 
                             (targetPosition - activeCamera.transform.position).normalized * maxDistance;
        }

        //Vector3 targetFlat = new(targetPosition.x, initialHandElevation, targetPosition.z); // Maintain initial elevation
        float step = Mathf.Min(maxHandSpeed * Time.deltaTime, Vector3.Distance(handAnchor.position, targetPosition));
        handAnchor.position = Vector3.MoveTowards(handAnchor.position, targetPosition, step);
        Debug.DrawLine(handAnchor.position, targetPosition, Color.red); // Draw a line for debugging

        if (hand3DInstance != null)
        {
            hand3DInstance.transform.position = handAnchor.position;
        }
    }

    void HandleRotation()
    {
        float rotationDelta = 0f;

        rotationDelta += Input.GetAxis("Rotation") * 1.5f; // Q and E keys for rotation by default

        if (Input.mouseScrollDelta.y != 0)
            rotationDelta += Input.mouseScrollDelta.y * rotationSpeed * Time.deltaTime * 10f;

        savedRotation *= Quaternion.Euler(0f, rotationDelta, 0f); // Update savedRotation

        if (heldRb != null && hand3DInstance != null)
        {
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
    #endregion
    #region Helper Methods
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
    void ShowLockSprite(Vector3 position)
    {
        if (lockSpritePrefab == null || uiCanvas == null) return;

        // Instantiate the lock sprite as a UI element
        GameObject lockSprite = Instantiate(lockSpritePrefab, uiCanvas.transform);
        LockSpriteFollower follower = lockSprite.AddComponent<LockSpriteFollower>();
        follower.Initialize(activeCamera, position, 1f); // Pass camera, world position, and fade duration
    }

    private Bucket IsMousePointingAtBucket()
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            if (hit.collider.CompareTag("Bucket")) // Check if the hit object has the "Bucket" tag
            {
                Bucket bucket = hit.collider.GetComponent<Bucket>();
                if (bucket != null)
                {
                    return hit.collider.GetComponent<Bucket>(); // Return the bucket GameObject
                }
            }
        }
        return null;
    }
    #endregion

    #region Hand Management

    private void CreateHandAnchor(Vector3 spawnPos)
    {
        GameObject anchorObject = new("HandAnchor");
        handAnchor = anchorObject.transform;
        handAnchor.position = spawnPos + new Vector3(0f, 0.5f, 0f);
    }

    private void AttachDominoToAnchor()
    {
        // heldDomino.transform.position = handAnchor.position;
        // heldDomino.transform.rotation = Quaternion.Euler(0f, savedRotation.eulerAngles.y, 0f); // Ensure x and z rotations are set to 0

        SpringJoint spring = heldDomino.AddComponent<SpringJoint>();
        spring.connectedBody = handAnchor.gameObject.AddComponent<Rigidbody>();
        spring.connectedBody.isKinematic = true; // Make the anchor kinematic
        spring.connectedBody.useGravity = false; // Disable gravity on the anchor
        spring.anchor = DominoLike.holdPoint;
        anchor = spring.anchor;
        spring.autoConfigureConnectedAnchor = false;
        spring.connectedAnchor = Vector3.zero;
        spring.tolerance = 0.001f;
        spring.damper = 1f;
        spring.massScale = 1f;
        spring.spring = 50f;
        // Tween the spring.spring value to 500 over 0.5 seconds using DOTween
        if (springTween != null && springTween.IsActive())
        {
            springTween.Kill(); // Kill the tween to clean it up
        }
        springTween = DOTween.To(() => spring.spring, x => spring.spring = x, 500f, 0.25f);

        heldRb.velocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
    }

    private void Create3DHand(Vector3 spawnPos)
    {
        if (hand3DPrefab == null) return;

        hand3DInstance = Instantiate(hand3DPrefab);
        hand3DInstance.transform.position = spawnPos;
        hand3DInstance.transform.rotation = Quaternion.Euler(
            hand3DInstance.transform.rotation.eulerAngles.x,
            savedRotation.eulerAngles.y,
            hand3DInstance.transform.rotation.eulerAngles.z
        );
    }

    private void DestroyHand()
    {
        if (springTween != null && springTween.IsActive())
        {
            springTween.Kill(); // Kill the tween to clean it up
        }

        if (handAnchor != null) Destroy(handAnchor.gameObject);

        if (hand3DInstance != null)
        {
            // Store the final rotation of the 3D hand before destroying it
            savedRotation = hand3DInstance.transform.rotation;
            Destroy(hand3DInstance);
        }
    }
    #endregion

    #region Flick Domino
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
            rb.AddForceAtPosition(forceDirection * rb.mass, worldHoldPoint, ForceMode.Impulse);
        }
    }
    #endregion
}