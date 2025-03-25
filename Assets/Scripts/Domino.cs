using UnityEngine;
using System.Collections;

public class Domino : MonoBehaviour
{
    private Rigidbody rb;
    private float stillnessThreshold = 5f;  // Velocity threshold to consider "stationary"
    public Vector3 holdPoint; // Offset from center to hold the domino
    public DominoSoundManager soundManager;
    public CameraController cameraController;
    public bool isMoving = false;
    public bool isHeld = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        soundManager = FindObjectOfType<DominoSoundManager>(); // Get reference
        cameraController = FindObjectOfType<CameraController>();
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
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        }
        else if (!currentlyMoving && isMoving)
        {
            isMoving = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        isMoving = currentlyMoving;
    }

    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
