using UnityEngine;
using System.Collections;
using DG.Tweening;

[SelectionBase]
public class Domino : MonoBehaviour
{
    private Rigidbody rb;
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
    private static float uprightThreshold = -0.97f; // How upright the domino must be (1 = perfectly upright)

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        // SaveStablePosition();
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
            CheckStability();
            // rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        }
        else if (!currentlyMoving && isMoving)
        {
            isMoving = false;
            // rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        isMoving = currentlyMoving;
    }

    private void CheckStability()
    {
        // Debug.Log("Difference between up and forward: " + Vector3.Dot(transform.forward, Vector3.up));
        if (!isHeld && Vector3.Dot(transform.forward, Vector3.up) < uprightThreshold)
        {
            SaveStablePosition();
        }
    }

    private void SaveStablePosition()
    {
        // if (!stablePositionSet) Debug.Log($"Saving stable position for {gameObject.name} at {transform.position} and {transform.rotation}");
        stablePositionSet = true;
        lastStablePosition = transform.position;
        lastStableRotation = transform.rotation;
        //Make sure stable rotation is perfectly upright
        // lastStableRotation = Quaternion.Euler(transform.eulerAngles.x, 0f, transform.eulerAngles.z);
    }
    private void OnCollisionEnter(Collision collision)
    {
        // Debug.Log($"Collision with {collision.gameObject.name}");
        float impactForce = collision.relativeVelocity.magnitude;
        CheckStability();
        if (impactForce < 1f) return; // Ignore small impacts

        DominoSoundManager.Instance.PlayDominoSound(impactForce, musicMode, audioSource);
        DominoResetManager.Instance.RegisterDomino(this, lastStablePosition, lastStableRotation);
    }

    public void ResetDomino()
    {
        transform.DOKill(); // Ensure any previous animations are cleared
        TogglePhysics(false);

        float jumpHeight = 3f;  // Height of the pop-up
        float duration = 1f;      // Total animation duration

        // Create a sequence for the reset animation
        Sequence resetSequence = DOTween.Sequence();

        // Move the domino upwards first
        resetSequence.Append(transform.DOJump(lastStablePosition, jumpHeight, 1, duration)
            .SetEase(Ease.OutQuad)); // Smooth easing

        // Rotate back to upright while moving up
        resetSequence.Join(transform.DORotateQuaternion(lastStableRotation, duration * 0.7f)
            .SetEase(Ease.OutExpo)); // Smooth rotation

        // Optionally add a small bounce effect at the end
        resetSequence.Append(transform.DOShakePosition(0.2f, 0.05f));

        // Play the sequence
        resetSequence.Play().OnComplete(() => TogglePhysics(true));
    }


    public void TogglePhysics(bool value)
    {
        rb.isKinematic = !value;
        GetComponent<BoxCollider>().enabled = value;
    }
    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
