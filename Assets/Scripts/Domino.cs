using UnityEngine;
using System.Collections;

public class Domino : MonoBehaviour
{
    private Rigidbody rb;
    private float stillnessThreshold = 0.2f;  // Velocity threshold to consider "stationary"
    public Vector3 holdPoint; // Offset from center to hold the domino
    public DominoSoundManager soundManager;
    public bool isMoving = false;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        soundManager = FindObjectOfType<DominoSoundManager>(); // Get reference
        soundManager.allDominoes.Add(this); // Register this domino
        soundManager?.StartCoroutine(soundManager.UpdateTargetVolume());
    }

    void OnDestroy()
    {
        soundManager?.allDominoes.Remove(this); // Unregister this domino
    }

    void Update()
    {
        bool currentlyMoving = rb.velocity.sqrMagnitude >= stillnessThreshold * stillnessThreshold || 
        rb.angularVelocity.sqrMagnitude >= stillnessThreshold * stillnessThreshold;

        if (currentlyMoving && !isMoving)
        {
            isMoving = true;
            // soundManager?.RegisterMovingDomino(); // Notify sound manager
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
        else if (!currentlyMoving && isMoving)
        {
            isMoving = false;
            // soundManager?.UnregisterMovingDomino(); // Notify sound manager
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        isMoving = currentlyMoving;
    }

}
