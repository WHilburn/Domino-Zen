using UnityEngine;
using System.Collections;
using DG.Tweening;
using System;

[SelectionBase]
public class Domino : MonoBehaviour
{
    private Rigidbody rb;
    public enum DominoState
    {
        Stable,
        Falling,
        Resetting
    }

    public enum ResetAnimation
    {
        Rotate,
        Jump,
        Reverse
    }
    private static float stillnessThreshold = 5f;  // Velocity threshold to consider "stationary"
    public Vector3 holdPoint; // Offset from center to hold the domino
    static DominoSoundManager soundManager;
    static CameraController cameraController;
    public bool isMoving = false;
    public bool isHeld = false;
    public float velocityMagnitude;
    public bool musicMode = true;
    private AudioSource audioSource;
    [HideInInspector]
    public bool stablePositionSet = false;
    private Vector3 lastStablePosition;
    private Quaternion lastStableRotation;
    private static float uprightThreshold = -0.99f; // How upright the domino must be (1 = perfectly upright)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        CheckStability();
        // InvokeRepeating(nameof(CheckStability), stabilityCheckDelay, stabilityCheckDelay);
    }

    void OnDestroy()
    {
        cameraController?.fallingDominoes.Remove(transform);
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

        if (currentlyMoving && !isMoving)
        {
            isMoving = true;
            if (!cameraController.fallingDominoes.Contains(transform))
            {
                cameraController.fallingDominoes.Add(transform);
            }
            StartCoroutine(RemoveFromFallingDominoes(0.25f));
            // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        }
        else if (!currentlyMoving && isMoving)
        {
            // CheckStability();
            isMoving = false;
            // rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        isMoving = currentlyMoving;
    }

    private void CheckStability()
    {
        if (rb.angularVelocity.magnitude > stillnessThreshold || rb.velocity.magnitude > stillnessThreshold)
        {
            // Check again after a short delay
            Invoke(nameof(CheckStability), .1f);
            return;
        }
        //Debug.Log("Difference between up and forward: " + Vector3.Dot(transform.forward, Vector3.up));
        if (!isHeld && Vector3.Dot(transform.forward, Vector3.up) < uprightThreshold)
        {
            SaveStablePosition();
        }
    }

    private void SaveStablePosition()
    {
        Debug.Log($"Saving stable position for {gameObject.name} at {transform.position} and {transform.rotation}");
        stablePositionSet = true;
        lastStablePosition = transform.position;
        lastStableRotation = transform.rotation;
        //Make sure stable rotation is perfectly upright
        lastStableRotation = Quaternion.Euler(90f, transform.eulerAngles.y, transform.eulerAngles.z);
    }
    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < 2f) return; // Ignore small impacts

        DominoSoundManager.Instance.PlayDominoSound(impactForce, musicMode, audioSource);
        DominoResetManager.Instance.RegisterDomino(this, lastStablePosition, lastStableRotation);
    }

    public void ResetDomino(ResetAnimation animation)
    {
        transform.DOKill(); // Ensure any previous animations are cleared
        if (isHeld) return; // Don't reset if the domino is being held
        TogglePhysics(false);

        if (!stablePositionSet) {
            Destroy(gameObject);
            return;
        }
        //If animation is set to rotate or the distance from the stable position is really small...
        if (animation == ResetAnimation.Rotate || Vector3.Distance(transform.position, lastStablePosition) < 0.3f)
        {
            float resetDuration = 1f;
            rb.transform.DOMove(lastStablePosition, resetDuration);
            rb.transform.DORotateQuaternion(lastStableRotation, resetDuration).OnComplete(() => TogglePhysics(true));
        }
        else if (animation == ResetAnimation.Jump)
        {
            float jumpHeight = 1.5f;  // Height of the pop-up
            float jumpDuration = 0.3f;  // Faster upward motion
            float fallDuration = 0.2f; // Faster downward motion
            float rotationDuration = 0.4f; // Smooth rotation time

            // Create a sequence for the reset animation
            Sequence jumpSequence = DOTween.Sequence();

            // Move the domino upwards quickly (Y-axis only)
            jumpSequence.Append(transform.DOMoveY(lastStablePosition.y + jumpHeight, jumpDuration)
                .SetEase(Ease.OutQuad)); // Smooth ascent

            // Rotate back to upright during ascent
            jumpSequence.Join(transform.DORotateQuaternion(lastStableRotation, rotationDuration)
                .SetEase(Ease.OutQuad)); // Smooth rotation

            // Tween the full position (X, Y, Z) back to the stable position during the fall
            jumpSequence.Append(transform.DOMove(lastStablePosition, fallDuration)
                .SetEase(Ease.InQuad)); // Smooth fall

            // Explicitly set the final position and rotation to ensure accuracy
            jumpSequence.OnComplete(() =>
            {
                transform.position = lastStablePosition;
                transform.rotation = lastStableRotation;
                TogglePhysics(true);
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
    }
    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
