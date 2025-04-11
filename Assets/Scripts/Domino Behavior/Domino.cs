using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.Events;

[SelectionBase]
public class Domino : DominoLike
{
    #region Fields and Enums
    [Header("Domino Settings")]
    private Rigidbody rb;

    public enum DominoAnimation
    {
        Rotate,
        Jump,
        Teleport,
        Jiggle
    }

    private static readonly float stillnessVelocityThreshold = 8f;  // Velocity threshold to consider "stationary"
    private static readonly float stillnessRotationThreshold = 6f;  // Rotation threshold to consider "stationary"

    public enum DominoState
    {
        Stationary,
        FillingIndicator,
        Moving,
        Held,
        Animating
    }

    public DominoState currentState = DominoState.Stationary;
    [HideInInspector]
    public float velocityMagnitude;
    public bool musicMode = true;
    [HideInInspector]
    public bool stablePositionSet = false;
    [HideInInspector]
    public PlacementIndicator placementIndicator; // Reference to the placement indicator the domino is placed inside

    public bool locked = false; // Flag to prevent player pickup when locked
    public Vector3 lastStablePosition;
    public Quaternion lastStableRotation;
    private static float uprightThreshold = 0.99f; // How upright the domino must be (1 = perfectly upright)
    public bool canSetNewStablePosition = true; // Flag to prevent multiple stability checks

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

        // Check for collisions with other dominoes at the origin position
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.05f); // Small radius around the origin
        foreach (var collider in colliders)
        {
            if (collider.gameObject != gameObject && collider.CompareTag("DominoTag"))
            {
                Debug.LogWarning($"Domino {name} is colliding on start with another domino: {collider.name}");
                DestroyImmediate(gameObject); // Destroy this domino if it collides with another one
            }
        }

        if (currentState != DominoState.Held)
        {
            SnapToGround();
            SaveStablePosition();
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

        UpdateVelocityMagnitude();
        HandleMovementState();
        HandlePlacementIndicatorState();

        // Draw a debug line from the domino to its last stable position
        Debug.DrawLine(transform.position, lastStablePosition, Color.red);
    }
    #endregion

    #region State Management
    private void UpdateVelocityMagnitude()
    {
        if (rb != null)
        {
            velocityMagnitude = rb.angularVelocity.magnitude;
        }
    }

    private void HandleMovementState()
    {
        if (currentState == DominoState.Animating) return; // Skip state updates if animating
        bool currentlyMoving = IsDominoMoving();

        if (currentlyMoving && currentState != DominoState.Moving) // When it starts moving
        {
            if (currentState == DominoState.Stationary || currentState == DominoState.FillingIndicator)
            {
                OnDominoFall.Invoke(this);
            }

            currentState = DominoState.Moving;
        }
        else if (!currentlyMoving && currentState == DominoState.Moving) // When it stops moving
        {
            currentState = DominoState.Stationary;
            OnDominoStopMoving.Invoke(this);
        }
    }

    private void HandlePlacementIndicatorState()
    {
        if (!IsDominoMoving() && placementIndicator != null && !locked)
        {
            currentState = DominoState.FillingIndicator;
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
    }

    public void AnimateDomino(DominoAnimation animation, float resetDuration = 1f)
    {
        if (currentState == DominoState.Held) return; // Don't reset if the domino is being held

        currentState = DominoState.Animating; // Set state to animating
        if (DOTween.IsTweening(transform)) return; // Prevent additional animations if a tween is active

        PerformAnimation(animation, resetDuration);
    }
    #endregion

    #region Animation Methods
    private void PerformAnimation(DominoAnimation animation, float resetDuration)
    {
        switch (animation)
        {
            case DominoAnimation.Teleport:
                PerformTeleport();
                break;
            case DominoAnimation.Rotate:
                PerformRotate(resetDuration);
                break;
            case DominoAnimation.Jump:
                PerformJump(resetDuration);
                break;
            case DominoAnimation.Jiggle:
                PerformJiggle();
                break;
        }
    }

    private void PerformTeleport()
    {
        rb.transform.position = lastStablePosition;
        rb.transform.rotation = lastStableRotation;
        currentState = DominoState.Stationary; // Reset state to stationary
        StartCoroutine(TogglePhysics(true));
    }

    private void PerformRotate(float resetDuration)
    {
        StartCoroutine(TogglePhysics(false));
        rb.transform.DOMove(lastStablePosition, resetDuration);
        rb.transform.DORotateQuaternion(lastStableRotation, resetDuration).OnComplete(() =>
        {
            PerformTeleport();
        });
    }

    private void PerformJump(float resetDuration)
    {
        float jumpHeight = 1.5f;  // Height of the pop-up
        float jumpDuration = 0.4f * resetDuration;  // Faster upward motion
        float fallDuration = 0.2f * resetDuration; // Faster downward motion
        float rotationDuration = 0.4f * resetDuration; // Smooth rotation time

        // Determine the upward jump target
        Vector3 jumpPeak = new Vector3(lastStablePosition.x, lastStablePosition.y + jumpHeight, lastStablePosition.z);

        // Random rotation to add a bit of flair
        Vector3 randomFlip = new Vector3(Random.Range(0f, 720f),Random.Range(0f, 720f),Random.Range(0f, 720f));

        StartCoroutine(TogglePhysics(false));
        Sequence jumpSequence = DOTween.Sequence();
        jumpSequence.Append(transform.DOMove(jumpPeak, jumpDuration).SetEase(Ease.OutQuad));
        jumpSequence.Join(transform.DORotate(randomFlip, jumpDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        jumpSequence.Append(transform.DORotateQuaternion(lastStableRotation, rotationDuration).SetEase(Ease.OutQuad));
        jumpSequence.Append(transform.DOMove(lastStablePosition, fallDuration).SetEase(Ease.InQuad));
    
        jumpSequence.OnComplete(() =>
        {
            PerformTeleport();
        });
        jumpSequence.Play();
    }

    private void PerformJiggle()
    {
        float jiggleDuration = 0.2f;
        float noiseIntensity = 0.1f; // Intensity of the jiggle movement
        Vector3 originalPosition = lastStablePosition;

        StartCoroutine(TogglePhysics(false));

        // Create a sequence for the jiggle animation
        Sequence jiggleSequence = DOTween.Sequence();

        // Add jiggle movement in a direction relative to the domino's current facing
        Vector3 rightDirection = transform.right * noiseIntensity; // Right relative to the domino's facing
        jiggleSequence.Append(transform.DOMove(originalPosition + rightDirection, jiggleDuration / 4).SetEase(Ease.InOutSine));
        jiggleSequence.Append(transform.DOMove(originalPosition - rightDirection, jiggleDuration / 2).SetEase(Ease.InOutSine));
        jiggleSequence.Append(transform.DOMove(originalPosition, jiggleDuration / 4).SetEase(Ease.InOutSine));

        // Ensure the position is reset to the original position at the end
        jiggleSequence.OnComplete(() =>
        {
            transform.position = originalPosition;
            StartCoroutine(TogglePhysics(true));
        });

        jiggleSequence.Play();
    }
    #endregion

    #region Collision Handling
    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < 0.5f || rb.isKinematic || currentState == DominoState.Animating) return; // Ignore small impacts

        OnDominoImpact.Invoke(this, impactForce, transform.position);

        if (currentState != DominoState.Held && collision.gameObject.CompareTag("DominoTag"))
        {
            if (currentState == DominoState.Stationary || currentState == DominoState.FillingIndicator)
            {
                OnDominoFall.Invoke(this);
            }
            currentState = DominoState.Moving;
        }
    }
    #endregion

    #region Physics Management
    public IEnumerator TogglePhysics(bool value)
    {
        // Stop any active DOTween animations
        transform.DOKill();

        if (value) yield return null; // Wait for the next frame to reenable physics
        rb.isKinematic = !value;

        if (value)
        {
            rb.velocity = Vector3.zero;
            rb.angularVelocity = Vector3.zero;
        }

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        boxCollider.enabled = value;

        if (!IsDominoMoving() && currentState != DominoState.Animating)
        {
            currentState = DominoState.Stationary;
        }
    }
    #endregion
}
