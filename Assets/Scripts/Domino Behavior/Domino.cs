using UnityEngine;
using System.Collections;
using DG.Tweening;

[SelectionBase]
public class Domino : MonoBehaviour
{
    private Rigidbody rb;
    public enum ResetAnimation
    {
        Rotate,
        Jump,
        Teleport
    }
    private static float stillnessThreshold = 5f;  // Velocity threshold to consider "stationary"
    public Vector3 holdPoint; // Offset from center to hold the domino
    static DominoSoundManager soundManager;
    static CameraController cameraController;
    public bool isMoving = false;
    public bool isHeld = false;
    public float velocityMagnitude;
    public bool musicMode = true;
    [HideInInspector]
    public bool stablePositionSet = false;
    private Vector3 lastStablePosition;
    private Quaternion lastStableRotation;
    private static float uprightThreshold = -0.99f; // How upright the domino must be (1 = perfectly upright)
    static float stabilityCheckDelay = 0.5f; // Delay between stability checks

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        CheckStability();
        InvokeRepeating(nameof(CheckStability), stabilityCheckDelay + UnityEngine.Random.Range(0f, .1f), stabilityCheckDelay);
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
        rb.angularVelocity.sqrMagnitude >= stillnessThreshold * stillnessThreshold;

        if (currentlyMoving && !isMoving) // When we start moving
        {
            isMoving = true;
            
            if (!cameraController.fallingDominoes.Contains(transform))
            {
                cameraController.fallingDominoes.Add(transform);
            }
            if (!rb.isKinematic)
            {
                // Debug.Log($"Registering domino {gameObject.name} at {transform.position} and {transform.rotation} through Update");
                DominoResetManager.Instance.RegisterDomino(this, lastStablePosition, lastStableRotation);
            }
            StartCoroutine(RemoveFromFallingDominoes(0.25f));
            // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        }
        else if (!currentlyMoving && isMoving) //When we stop moving
        {
            CheckStability();
            isMoving = false;
            // rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        isMoving = currentlyMoving;
    }

    private void CheckStability()
    {
        if (rb.isKinematic  || 
        lastStablePosition == transform.position || 
        rb.angularVelocity.magnitude > stillnessThreshold || 
        rb.velocity.magnitude > stillnessThreshold)
        {
            return;
        }
        //Debug.Log("Difference between up and forward: " + Vector3.Dot(transform.forward, Vector3.up));
        if (!isHeld && Vector3.Dot(transform.forward, Vector3.up) < uprightThreshold)
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

    private void SaveStablePosition()
    {
        // Debug.Log($"Saving stable position for {gameObject.name} at {transform.position} and {transform.rotation}");
        stablePositionSet = true;
        lastStablePosition = transform.position;
        //Make sure stable rotation is perfectly upright
        lastStableRotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);
    }
    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < .5f || rb.isKinematic) return; // Ignore small impacts

        DominoSoundManager.Instance.PlayDominoSound(impactForce, musicMode);
        // Debug.Log($"Registering domino {gameObject.name} at {transform.position} and {transform.rotation} through Colision");
        DominoResetManager.Instance.RegisterDomino(this, lastStablePosition, lastStableRotation);
    }

    public void ResetDomino(ResetAnimation animation)
    {
        if (isHeld) return; // Don't reset if the domino is being held
        if (!stablePositionSet) {
            DespawnDomino();
            return;
        }
        if (animation == ResetAnimation.Teleport)
        {
            // Debug.Log($"Teleporting domino {gameObject.name} to stable position at {lastStablePosition} and {lastStableRotation}");
            rb.transform.position = lastStablePosition;
            rb.transform.rotation = lastStableRotation;
            TogglePhysics(true);
            return;
        }
        TogglePhysics(false);

        //If animation is set to rotate or the distance from the stable position is really small...
        if (animation == ResetAnimation.Rotate || Vector3.Distance(transform.position, lastStablePosition) < 0.5f)
        {
            // Debug.Log($"Resetting domino {gameObject.name} to stable position at {lastStablePosition} and {lastStableRotation}");
            float resetDuration = 1f;
            rb.transform.DOMove(lastStablePosition, resetDuration);
            rb.transform.DORotateQuaternion(lastStableRotation, resetDuration).OnComplete(() => 
            {
                TogglePhysics(true);
                StartCoroutine(RemoveFromFallingDominoes(0.25f));
            });
        }
        else if (animation == ResetAnimation.Jump)
        {
            float jumpHeight = 1.5f;  // Height of the pop-up
            float jumpDuration = 0.3f;  // Faster upward motion
            float fallDuration = 0.15f; // Faster downward motion
            float rotationDuration = 0.4f; // Smooth rotation time

            // Create a sequence for the reset animation
            Sequence jumpSequence = DOTween.Sequence();

            // Move the domino upwards quickly (Y-axis only)
            jumpSequence.Append(transform.DOMoveY(lastStablePosition.y + jumpHeight, jumpDuration)
                .SetEase(Ease.OutQuad)); // Smooth ascent

            // Add a random flipping rotation during the ascent
            Vector3 randomFlip = new Vector3(
                Random.Range(0f, 720f), // Random X-axis rotation (up to 2 full flips)
                Random.Range(0f, 720f), // Random Y-axis rotation
                Random.Range(0f, 720f)  // Random Z-axis rotation
            );
            // Debug.Log($"Random flip rotation: {randomFlip}");
            jumpSequence.Join(transform.DORotate(randomFlip, jumpDuration, RotateMode.FastBeyond360)
                .SetEase(Ease.OutQuad)); // Smooth flipping rotation

            // Ensure the upright rotation happens after the flip
            jumpSequence.Append(transform.DORotateQuaternion(lastStableRotation, rotationDuration)
                .SetEase(Ease.OutQuad)); // Smooth rotation back to upright

            // Tween the full position (X, Y, Z) back to the stable position during the fall
            jumpSequence.Append(transform.DOMove(lastStablePosition, fallDuration)
                .SetEase(Ease.InQuad)); // Smooth fall

            // Explicitly set the final position and rotation to ensure accuracy
            jumpSequence.OnComplete(() =>
            {
                transform.position = lastStablePosition;
                transform.rotation = lastStableRotation;
                TogglePhysics(true);
                StartCoroutine(RemoveFromFallingDominoes(0.1f));
            });

            // Play the sequence
            jumpSequence.Play();
        }
    }


    public void TogglePhysics(bool value)
    {
        rb.isKinematic = !value;
        // rb.useGravity = value;
        GetComponent<BoxCollider>().enabled = value;
        transform.DOKill();
    }
    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
