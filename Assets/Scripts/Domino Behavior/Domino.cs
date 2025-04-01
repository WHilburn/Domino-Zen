using UnityEngine;
using System.Collections;

[SelectionBase]
public class Domino : MonoBehaviour
{
    private Rigidbody rb;
    private float stillnessThreshold = 5f;  // Velocity threshold to consider "stationary"
    public Vector3 holdPoint; // Offset from center to hold the domino
    static DominoSoundManager soundManager;
    static CameraController cameraController;
    public bool isMoving = false;
    public bool isHeld = false;
    public float velocityMagnitude;
    public bool musicMode = true;
    private AudioSource audioSource;
    private Vector3 lastStablePosition;
    private Quaternion lastStableRotation;
    private float stabilityCheckDelay = 1f; // Time before checking stability
    private float uprightThreshold = 0.95f; // How upright the domino must be (1 = perfectly upright)
    private float velocityThreshold = 0.1f; // Speed below which the domino is considered stable

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
        if (cameraController == null) cameraController = FindObjectOfType<CameraController>();
        // SaveStablePosition();
        InvokeRepeating(nameof(CheckStability), stabilityCheckDelay, stabilityCheckDelay);
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
            isMoving = false;
            // rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        isMoving = currentlyMoving;
    }

    private void CheckStability()
    {
        Debug.Log("Difference between up and forward: " + Vector3.Dot(transform.forward, Vector3.up));
        if (rb.velocity.magnitude < velocityThreshold && rb.angularVelocity.magnitude < velocityThreshold 
        && Vector3.Dot(transform.forward, Vector3.up) > uprightThreshold)
        {
            SaveStablePosition();
        }
    }

    private void SaveStablePosition()
    {
        Debug.Log($"Saving stable position for {gameObject.name} at {transform.position}");
        lastStablePosition = transform.position;
        lastStableRotation = transform.rotation;
    }
    private void OnCollisionEnter(Collision collision)
    {
        // Debug.Log($"Collision with {collision.gameObject.name}");
        float impactForce = collision.relativeVelocity.magnitude;
        if (impactForce < 1f) return; // Ignore small impacts

        DominoSoundManager.Instance.PlayDominoSound(impactForce, musicMode, audioSource);
        if (impactForce > 1f && rb.velocity.magnitude > velocityThreshold && Vector3.Dot(transform.up, Vector3.up) < uprightThreshold)
        {
            DominoResetManager.Instance.RegisterDomino(rb, lastStablePosition, lastStableRotation);
        }
    }
    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
