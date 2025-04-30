using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.Events;

[SelectionBase]
public class Domino : DominoLike
{
    #region Fields and Enums
    [Header("Domino Settings")]
    [HideInInspector] public Rigidbody rb;
    public DominoSoundManager.DominoSoundType soundType; // Sound type for the domino

    public enum DominoAnimation
    {
        Rotate,
        Teleport,
        Jiggle,
        Jump
    }

    private static readonly float stillnessVelocityThreshold = 6f;  // Velocity threshold to consider "stationary"
    private static readonly float stillnessRotationThreshold = 6f;  // Rotation threshold to consider "stationary"

    public enum DominoState
    {
        Stationary,
        Moving,
        Held,
        Animating
    }

    public DominoState currentState = DominoState.Stationary;
    public bool stablePositionSet = false;
    [HideInInspector]
    public PlacementIndicator placementIndicator; // Reference to the placement indicator the domino is placed inside

    public bool locked = false; // Flag to prevent player pickup when locked
    public Vector3 lastStablePosition;
    public Quaternion lastStableRotation;
    private static float uprightThreshold = 0.99f; // How upright the domino must be (1 = perfectly upright)

    // Unity Events
    public static UnityEvent<Domino> OnDominoFall = new();
    public static UnityEvent<Domino> OnDominoStopMoving = new();
    public static UnityEvent<Domino> OnDominoPlacedCorrectly = new();
    public static UnityEvent<Domino> OnDominoCreated = new();
    public static UnityEvent<Domino> OnDominoDeleted = new();
    public static UnityEvent<Domino, float, Vector3> OnDominoImpact = new();
    #endregion

    #region Unity Lifecycle Methods
    void Start()
    {
        OnDominoCreated.Invoke(this); // Notify listeners of domino creation
        rb = GetComponent<Rigidbody>();
         // Check for overlaps with other dominoes

        if (!CheckAndResolveOverlap() && currentState != DominoState.Held)
        {
            SnapToGround();
            SaveStablePosition(); //Save stable state if the domino exists at the start of the scene
        }
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnDestroy()
    {
        OnDominoDeleted.Invoke(this); // Notify listeners of domino deletion
    }

    void Update()
    {
        if (currentState == DominoState.Held)
        {
            return;
        }

        HandleMovementState();
        // Draw a debug line from the domino to its last stable position
        // Debug.DrawLine(transform.position, lastStablePosition, Color.red);
    }
    #endregion

    #region State Management

    private void HandleMovementState()
    {
        if (currentState == DominoState.Animating) return; // Skip state updates if animating
        bool currentlyMoving = IsDominoMoving();

        if (currentlyMoving && currentState != DominoState.Moving) // When it starts moving
        {
            if (currentState == DominoState.Stationary && SceneLoader.asyncLoad == null)
            {
                // Debug.Log("Domino is falling because the movement state changed to Moving");
                OnDominoFall.Invoke(this);
                StopCoroutine(CheckStablePositionRoutine());
            }

            currentState = DominoState.Moving;
        }
        else if (!currentlyMoving && currentState == DominoState.Moving && SceneLoader.asyncLoad == null) // When it stops moving
        {
            currentState = DominoState.Stationary;
            OnDominoStopMoving.Invoke(this);
        }
    }

    private bool IsDominoMoving()
    {
        return rb.velocity.magnitude >= stillnessVelocityThreshold ||
               rb.angularVelocity.magnitude >= stillnessRotationThreshold;
    }
    #endregion

    #region Public Methods
    public bool CheckUpright()
    {
        return Vector3.Dot(transform.up, Vector3.up) > uprightThreshold && rb.angularVelocity.magnitude < stillnessVelocityThreshold;
    }

    public void DespawnDomino()
    {
        currentState = DominoState.Animating; // Set state to animating
        StartCoroutine(TogglePhysics(false)); // Disable collisions with other dominoes

        // Scale the domino down to zero
        float scaleDuration = 0.5f; // Duration of the scaling animation
        transform.DOScale(Vector3.zero, scaleDuration)
            .SetEase(Ease.OutSine) // Smooth scaling effect
            .OnComplete(() => Destroy(gameObject));
    }

    public void SaveStablePosition(Transform inputTransform = null)
    {
        stablePositionSet = true;

        if (inputTransform != null)
        {
            lastStablePosition = inputTransform.position;
            lastStableRotation = inputTransform.rotation;
        }
        else
        {
            lastStablePosition = transform.position;
            lastStableRotation = transform.rotation;
        }

        StartCoroutine(CheckStablePositionRoutine());
    }

    private IEnumerator CheckStablePositionRoutine()
    {
        const float positionThreshold = 0.05f; // Threshold for position difference
        const float rotationThreshold = 1f; // Threshold for rotation difference in degrees
        float checkInterval = .5f; // Time interval between checks
        yield return new WaitForSeconds(Random.Range(1f,1.2f)); // Wait for the first check for a random time so not all dominoes check at the same time

        while (stablePositionSet)
        {
            yield return new WaitForSeconds(checkInterval);

            // Check position and rotation thresholds
            if (currentState == DominoState.Stationary &&
                SceneLoader.asyncLoad == null && 
                (Vector3.Distance(transform.position, lastStablePosition) > positionThreshold ||
                Quaternion.Angle(transform.rotation, lastStableRotation) > rotationThreshold))
            {
                // Debug.Log("Domino is not stable anymore.");
                OnDominoFall.Invoke(this);
                yield break; // Stop the coroutine
            }
        }
    }

    public void AnimateDomino(DominoAnimation animation, float resetDuration = 1f)
    {
        if (currentState == DominoState.Held || currentState == DominoState.Animating) return; // Don't reset if the domino is being held or already animating

        currentState = DominoState.Animating; // Set state to animating
        if (DOTween.IsTweening(transform)) return; // Prevent additional animations if a tween is active

        PerformAnimation(animation, resetDuration);
    }
    #endregion

    #region Animation Methods
    private void PerformAnimation(DominoAnimation animation, float resetDuration)
    {
        StartCoroutine(TogglePhysics(false)); // Disable collisions with other dominoes
        switch (animation)
        {
            case DominoAnimation.Teleport:
                PerformTeleport();
                break;
            case DominoAnimation.Rotate:
                PerformRotate(resetDuration);
                break;
            case DominoAnimation.Jiggle:
                PerformJiggle();
                break;
            case DominoAnimation.Jump:
                PerformJump(resetDuration, 1f);
                break;
        }
    }

    private void PerformTeleport()
    {
        rb.transform.position = lastStablePosition;
        rb.transform.rotation = lastStableRotation;
        StartCoroutine(TogglePhysics(true));
        StartCoroutine(CheckStablePositionRoutine());
    }

    private void PerformRotate(float resetDuration)
    {
        StartCoroutine(TogglePhysics(false));
        rb.transform.DOMove(lastStablePosition, resetDuration).SetUpdate(UpdateType.Fixed);
        rb.transform.DORotateQuaternion(lastStableRotation, resetDuration)
            .SetUpdate(UpdateType.Fixed)
            .OnComplete(() =>
            {
                PerformTeleport();
                currentState = DominoState.Stationary;
            });
    }

    private void PerformJiggle()
    {
        float jiggleDuration = 0.2f;
        float noiseIntensity = 0.1f; // Intensity of the jiggle movement
        Vector3 originalPosition = lastStablePosition;
        Quaternion originalRotation = lastStableRotation;
        if (!stablePositionSet){
            originalPosition = transform.position; // If no stable position, use current position
            originalRotation = transform.rotation; // If no stable rotation, use current rotation
        } 
        
        StartCoroutine(TogglePhysics(false));

        // Create a sequence for the jiggle animation
        DG.Tweening.Sequence jiggleSequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);

        // Add jiggle movement in a direction relative to the domino's current facing
        Vector3 rightDirection = transform.right * noiseIntensity; // Right relative to the domino's facing
        jiggleSequence.Append(rb.transform.DOMove(originalPosition + rightDirection, jiggleDuration / 4).SetEase(Ease.InOutSine));
        jiggleSequence.Append(rb.transform.DOMove(originalPosition - rightDirection, jiggleDuration / 2).SetEase(Ease.InOutSine));
        jiggleSequence.Append(rb.transform.DOMove(originalPosition, jiggleDuration / 4).SetEase(Ease.InOutSine));

        // Ensure the position is reset to the original position at the end
        jiggleSequence.OnComplete(() =>
        {
            rb.transform.position = originalPosition;
            rb.transform.rotation = originalRotation;
            currentState = DominoState.Stationary;
            StartCoroutine(TogglePhysics(true));
            StartCoroutine(CheckStablePositionRoutine());
        });

        jiggleSequence.Play();
    }

    private void PerformJump(float duration, float jumpHeight)
    {
        if (!stablePositionSet) return; // Ensure stable position is set
        Vector3 endPosition = lastStablePosition;
        float peakY = Mathf.Max(transform.position.y, endPosition.y) + jumpHeight;

        // Create a sequence for the jump animation
        DG.Tweening.Sequence jumpSequence = DOTween.Sequence().SetUpdate(UpdateType.Fixed);

        // Move laterally on x and z axes while rotating to the stable rotation
        jumpSequence.Append(rb.transform.DOMoveX(endPosition.x, duration).SetEase(Ease.InOutSine));
        jumpSequence.Join(rb.transform.DOMoveZ(endPosition.z, duration).SetEase(Ease.InOutSine));
        jumpSequence.Join(rb.transform.DORotateQuaternion(lastStableRotation, duration).SetEase(Ease.InOutSine));

        // Create a parabolic jump on the y-axis
        jumpSequence.Join(rb.transform.DOMoveY(peakY, duration / 2).SetEase(Ease.OutSine)); // Ascend
        jumpSequence.Append(rb.transform.DOMoveY(endPosition.y, duration / 2).SetEase(Ease.InSine)); // Descend

        // Ensure physics is re-enabled after the animation
        jumpSequence.OnComplete(() =>
        {
            PerformTeleport();
            currentState = DominoState.Stationary;
        });

        jumpSequence.Play();
    }
    #endregion

    #region Collision Handling
    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < 0.5f || rb.isKinematic || currentState == DominoState.Animating) return; // Ignore small impacts

        if (SceneLoader.asyncLoad == null) OnDominoImpact.Invoke(this, impactForce, transform.position);

        if (currentState != DominoState.Held && collision.gameObject.CompareTag("DominoTag"))
        {
            if (currentState == DominoState.Stationary)
            {
                // Debug.Log("Domino is falling because it had a collision");
                OnDominoFall.Invoke(this);
                StopCoroutine(CheckStablePositionRoutine());
            }
            currentState = DominoState.Moving;
        }
    }
    #endregion

    #region Physics Management
    public IEnumerator TogglePhysics(bool on)
    {
        if (on) 
        {
            // Stop any active DOTween animations
            rb.transform.DOKill();
            yield return new WaitForFixedUpdate(); // Wait for the next frame to reenable physics
        }

        rb.isKinematic = !on;

        if (on)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
            // currentState = DominoState.Stationary;
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.isTrigger = !on;
    }
    #endregion
}
