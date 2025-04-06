using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEngine.Events;

[SelectionBase]
public class Domino : DominoLike
{
    [Header("Domino Settings")]
    private Rigidbody rb;
    public enum DominoAnimation
    {
        Rotate,
        Jump,
        Teleport,
        Jiggle
    }
    private static readonly float stillnessVelocityThreshold = 5f;  // Velocity threshold to consider "stationary"
    private static readonly float stillnessRotationThreshold = 1f;  // Velocity threshold to consider "stationary"
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
    static float stabilityCheckDelay = 0.5f; // Delay between stability checks
    public bool canSetNewStablePosition = true; // Flag to prevent multiple stability checks
    public static UnityEvent<Domino> OnDominoFall = new(); // Domino calls this to register for reset, causing cascade sounds, etc
    public static UnityEvent<Domino> OnDominoStopMoving = new(); // Domino calls this to end cascade sounds
    public static UnityEvent<Domino> OnDominoPlacedCorrectly= new(); // Calls this to notify systems it's been placed in an indicator
    public static UnityEvent<Domino> OnDominoCreated = new(); // Calls this to notify systems of it's creation
    public static UnityEvent<Domino> OnDominoDeleted = new(); // Calls this to notify systems of it's deletion
    public static UnityEvent<Domino, float, Vector3> OnDominoImpact = new(); // Calls this to make sounds

    private LineRenderer lineRenderer;////////////////////////////////////////

    void Start()
    {
        OnDominoCreated.Invoke(this); // Notify listeners of domino creation
        rb = GetComponent<Rigidbody>();

        // Add and configure the LineRenderer ////////////////////////////////////////
        lineRenderer = gameObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.05f;
        lineRenderer.endWidth = 0.05f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default")); // Use a default material
        lineRenderer.startColor = Color.red;
        lineRenderer.endColor = Color.red;

        if (currentState != DominoState.Held)
        {
            SnapToGround();
            SaveStablePosition();
        }
        InvokeRepeating(nameof(CheckStability), stabilityCheckDelay + Random.Range(0f, .1f), stabilityCheckDelay);
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

        if (lineRenderer != null)
        {
            lineRenderer.SetPosition(0, transform.position); // Start point: current position
            lineRenderer.SetPosition(1, lastStablePosition); // End point: last stable position
        }

        if (rb != null)
        {
            velocityMagnitude = rb.angularVelocity.magnitude;
        }

        bool currentlyMoving = rb.velocity.magnitude >= stillnessVelocityThreshold || 
                               rb.angularVelocity.magnitude >= stillnessRotationThreshold;

        if (currentlyMoving && currentState != DominoState.Moving && currentState != DominoState.Animating) // When we start moving
        {
            if (currentState == DominoState.Stationary || currentState == DominoState.FillingIndicator) // Releasing a held domino does not count as "falling"
            {
                OnDominoFall.Invoke(this);
            }

            currentState = DominoState.Moving; // Set state to moving
        }
        else if (!currentlyMoving && currentState == DominoState.Moving) // When we stop moving
        {
            CheckStability();
            currentState = DominoState.Stationary;
            OnDominoStopMoving.Invoke(this); // Notify listeners of domino stopping
        }

        if (!currentlyMoving && placementIndicator != null && !locked)
        {
            currentState = DominoState.FillingIndicator;
        }
    }

    private void CheckStability()
    {
        if (currentState != DominoState.Stationary ||
            locked || 
            !canSetNewStablePosition ||
            rb.isKinematic || 
            lastStablePosition == transform.position || 
            rb.angularVelocity.magnitude > stillnessVelocityThreshold || 
            rb.velocity.magnitude > stillnessVelocityThreshold)
        {
            return;
        }

        if (Vector3.Dot(transform.up, Vector3.up) > uprightThreshold)
        {
            SaveStablePosition();
        }
    }

    public void DespawnDomino()
    {
        // Disable collisions with other dominoes
        TogglePhysics(false);

        // Scale the domino down to zero
        float scaleDuration = 0.5f; // Duration of the scaling animation
        transform.DOScale(Vector3.zero, scaleDuration)
            .SetEase(Ease.OutSine) // Smooth scaling effect
            .OnComplete(() =>
            {
                // Destroy the domino after scaling down
                Destroy(gameObject);
            });
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

    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < .5f || rb.isKinematic) return; // Ignore small impacts

        OnDominoImpact.Invoke(this, impactForce, transform.position);

        if (currentState != DominoState.Held)
        {
            if (currentState == DominoState.Stationary || currentState == DominoState.FillingIndicator) // Releasing a held domino does not count as "falling"
            {
                OnDominoFall.Invoke(this);
            }
            currentState = DominoState.Moving;
        }
    }

    public void AnimateDomino(DominoAnimation animation, float resetDuration = 1f)
    {
        if (currentState == DominoState.Held) return; // Don't reset if the domino is being held
        currentState = DominoState.Animating; // Set state to animating
        if (!stablePositionSet)
        {
            DespawnDomino();
            return;
        }
        if (DOTween.IsTweening(transform)) return; // Prevent additional animations if a tween is active

        bool savedSetting = canSetNewStablePosition;
        canSetNewStablePosition = false; // Prevent multiple stability checks during animation
        switch (animation)
        {
            case DominoAnimation.Teleport:
                PerformTeleport();
                break;
            case DominoAnimation.Rotate:
                PerformRotate(resetDuration);
                break;
            case DominoAnimation.Jump:
                PerformJump();
                break;
            case DominoAnimation.Jiggle:
                PerformJiggle();
                break;
        }
        canSetNewStablePosition = savedSetting; // Restore the ability to set new stable positions
        currentState = DominoState.Stationary; // Reset state to stationary
    }

    private void PerformTeleport()
    {
        rb.transform.position = lastStablePosition;
        rb.transform.rotation = lastStableRotation;
        TogglePhysics(true);
    }

    private void PerformRotate(float resetDuration)
    {
        TogglePhysics(false);
        rb.transform.DOMove(lastStablePosition, resetDuration);
        rb.transform.DORotateQuaternion(lastStableRotation, resetDuration).OnComplete(() =>
        {
            TogglePhysics(true);
        });
    }

    private void PerformJump()
    {
        float jumpHeight = 1.5f;  // Height of the pop-up
        float jumpDuration = 0.3f;  // Faster upward motion
        float fallDuration = 0.15f; // Faster downward motion
        float rotationDuration = 0.4f; // Smooth rotation time

        TogglePhysics(false);
        Sequence jumpSequence = DOTween.Sequence();
        jumpSequence.Append(transform.DOMoveY(lastStablePosition.y + jumpHeight, jumpDuration).SetEase(Ease.OutQuad));
        Vector3 randomFlip = new(Random.Range(0f, 720f), Random.Range(0f, 720f), Random.Range(0f, 720f));
        jumpSequence.Join(transform.DORotate(randomFlip, jumpDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        jumpSequence.Append(transform.DORotateQuaternion(lastStableRotation, rotationDuration).SetEase(Ease.OutQuad));
        jumpSequence.Append(transform.DOMove(lastStablePosition, fallDuration).SetEase(Ease.InQuad));
        jumpSequence.OnComplete(() =>
        {
            transform.position = lastStablePosition;
            transform.rotation = lastStableRotation;
            TogglePhysics(true);
        });
        jumpSequence.Play();
    }

    private void PerformJiggle()
    {
        float jiggleDuration = 0.2f;
        float noiseIntensity = 0.1f; // Intensity of the jiggle movement
        Vector3 originalPosition = lastStablePosition;

        TogglePhysics(false);

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
            TogglePhysics(true);
        });

        jiggleSequence.Play();
    }

    public void TogglePhysics(bool value)
    {
        if (rb == null)
        {
            Debug.LogError($"Rigidbody is missing on {gameObject.name}. Ensure the prefab has a Rigidbody component.");
            return;
        }

        rb.isKinematic = !value;

        BoxCollider boxCollider = GetComponent<BoxCollider>();
        if (boxCollider == null)
        {
            Debug.LogError($"BoxCollider is missing on {gameObject.name}. Ensure the prefab has a BoxCollider component.");
            return;
        }

        boxCollider.enabled = value;

        // Stop any active DOTween animations
        transform.DOKill();

        if (!value)
        {
            currentState = DominoState.Held;
        }
        else if (rb.velocity.sqrMagnitude < stillnessVelocityThreshold * stillnessVelocityThreshold &&
                 rb.angularVelocity.sqrMagnitude < stillnessVelocityThreshold * stillnessVelocityThreshold)
        {
            currentState = DominoState.Stationary;
        }
    }
}
