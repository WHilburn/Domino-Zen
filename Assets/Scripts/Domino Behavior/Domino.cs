using UnityEngine;
using System.Collections;
using DG.Tweening;
using UnityEditor;

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
    private static float stillnessThreshold = 5f;  // Velocity threshold to consider "stationary"
    static DominoSoundManager soundManager;
    static CameraController cameraController;
    public bool isMoving = false;
    public bool isHeld = false;
    [HideInInspector]
    public float velocityMagnitude;
    public bool musicMode = true;
    [HideInInspector]
    public bool stablePositionSet = false;
    [HideInInspector]
    public PlacementIndicator placementIndicator; // Reference to the placement indicator the domino is placed inside
    public bool locked = false; // Flag to prevent movement when locked
    private Vector3 lastStablePosition;
    private Quaternion lastStableRotation;
    private static float uprightThreshold = 0.99f; // How upright the domino must be (1 = perfectly upright)
    static float stabilityCheckDelay = 0.5f; // Delay between stability checks
    public bool canSetNewStablePosition = true; // Flag to prevent multiple stability checks

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        if (!isHeld){
            SnapToGround();
            SaveStablePosition();
        }
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        // CheckStability();
        InvokeRepeating(nameof(CheckStability), stabilityCheckDelay + Random.Range(0f, .1f), stabilityCheckDelay);
    }

    void Awake()
    {
        rb = GetComponent<Rigidbody>();
    }

    void OnDestroy()
    {
        cameraController?.fallingDominoes.Remove(transform);
        DominoResetManager.Instance.RemoveDomino(this); // Remove from reset manager
    }

    void Update()
    {
        if (isHeld)
        {
            return;
        }
        
        if (rb != null && soundManager != null)
        {
            velocityMagnitude = rb.angularVelocity.magnitude;
            soundManager.UpdateDominoMovement(velocityMagnitude);
        }

        bool currentlyMoving = rb.velocity.sqrMagnitude >= stillnessThreshold * stillnessThreshold || 
        rb.angularVelocity.sqrMagnitude >= stillnessThreshold * stillnessThreshold; // Use this to track if the "isMoving" state should change

        if (currentlyMoving && !isMoving) // When we start moving
        {
            isMoving = true;
            
            if (cameraController!= null && !cameraController.fallingDominoes.Contains(transform))
            {
                cameraController.fallingDominoes.Add(transform);
            }
            if (!rb.isKinematic)
            {
                // Debug.Log($"Registering domino {gameObject.name} at {transform.position} and {transform.rotation} through Update");
                DominoResetManager.Instance.RegisterDomino(this, lastStablePosition, lastStableRotation);
            }
            StartCoroutine(RemoveFromFallingDominoes(0.25f));

        }
        else if (!currentlyMoving && isMoving) //When we stop moving
        {
            CheckStability();
            isMoving = false;
        }
        isMoving = currentlyMoving;
    }

    private void CheckStability()
    {
        if (locked ||
        rb.isKinematic  || 
        lastStablePosition == transform.position || 
        rb.angularVelocity.magnitude > stillnessThreshold || 
        rb.velocity.magnitude > stillnessThreshold ||
        !canSetNewStablePosition)
        {
            return;
        }
        if (!isHeld && Vector3.Dot(transform.up, Vector3.up) > uprightThreshold)
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

    public void SaveStablePosition()
    {
        stablePositionSet = true;
        lastStablePosition = transform.position;
        lastStableRotation = transform.rotation;
        //Make sure stable rotation is perfectly upright
        // lastStableRotation = Quaternion.Euler(0f, transform.eulerAngles.y, transform.eulerAngles.z);
    }
    public void SetStablePosition(Transform inputTransform)
    {
        stablePositionSet = true;
        lastStablePosition = inputTransform.position;
        lastStableRotation = inputTransform.rotation;
    }
    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < .5f || rb.isKinematic) return; // Ignore small impacts

        if (musicMode)
        {
            DominoSoundManager.Instance.PlayDominoSound(impactForce, transform.position, DominoSoundManager.DominoSoundType.Piano);
        }
        else
        {
            DominoSoundManager.Instance.PlayDominoSound(impactForce, transform.position);
        }

        // Debug.Log($"Registering domino {gameObject.name} at world position {transform.position} and world rotation {transform.rotation.eulerAngles} through Collision");
        DominoResetManager.Instance.RegisterDomino(this, lastStablePosition, lastStableRotation);
    }

    public void AnimateDomino(DominoAnimation animation, float resetDuration = 1f)
    {
        if (isHeld) return; // Don't reset if the domino is being held
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
    }

    // Abstracted methods for each animation type
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
            StartCoroutine(RemoveFromFallingDominoes(0.25f));
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
        Vector3 randomFlip = new Vector3(Random.Range(0f, 720f), Random.Range(0f, 720f), Random.Range(0f, 720f));
        jumpSequence.Join(transform.DORotate(randomFlip, jumpDuration, RotateMode.FastBeyond360).SetEase(Ease.OutQuad));
        jumpSequence.Append(transform.DORotateQuaternion(lastStableRotation, rotationDuration).SetEase(Ease.OutQuad));
        jumpSequence.Append(transform.DOMove(lastStablePosition, fallDuration).SetEase(Ease.InQuad));
        jumpSequence.OnComplete(() =>
        {
            transform.position = lastStablePosition;
            transform.rotation = lastStableRotation;
            TogglePhysics(true);
            StartCoroutine(RemoveFromFallingDominoes(0.1f));
        });
        jumpSequence.Play();
    }

    private void PerformJiggle()
    {
        soundManager?.playArbitrarySound(soundManager.dominoLockedSound, 1, 1, transform.position);

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
        // jiggleSequence.Append(transform.DOMove(originalPosition + new Vector3(noiseIntensity, 0, 0), jiggleDuration / 4).SetEase(Ease.InOutSine));
        // jiggleSequence.Append(transform.DOMove(originalPosition + new Vector3(-noiseIntensity, 0, 0), jiggleDuration / 2).SetEase(Ease.InOutSine));
        // jiggleSequence.Append(transform.DOMove(originalPosition, jiggleDuration / 4).SetEase(Ease.InOutSine));

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
    }
    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
