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
            CheckStability();
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
        if (impactForce < 1f) return; // Ignore small impacts

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
            float fallDuration = 0.15f; // Faster downward motion
            float rotationDuration = 0.4f; // Smooth rotation time

            // Create a sequence for the reset animation
            Sequence resetSequence = DOTween.Sequence();

            // Move the domino upwards quickly (Y-axis only)
            resetSequence.Append(transform.DOMoveY(lastStablePosition.y + jumpHeight, jumpDuration)
                .SetEase(Ease.OutQuad)); // Quick ascent

            // Rotate back to upright during ascent
            resetSequence.Join(transform.DORotateQuaternion(lastStableRotation, rotationDuration)
                .SetEase(Ease.OutExpo));

            // Tween the full position (X, Y, Z) back to the stable position during the fall
            resetSequence.Append(transform.DOMove(lastStablePosition, fallDuration)
                .SetEase(Ease.InQuad)); // Snappy fall

            // Optional tiny bounce effect at the end
            // resetSequence.Append(transform.DOShakePosition(0.01f, 0.03f));
            // resetSequence.Append(transform.DOMove(lastStablePosition, 0.05f));

            // Play the sequence
            resetSequence.Play().OnComplete(() => TogglePhysics(true));
        }
    }


    public void TogglePhysics(bool value)
    {
        rb.isKinematic = !value;
        rb.useGravity = value;
        GetComponent<BoxCollider>().enabled = value;
    }
    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
