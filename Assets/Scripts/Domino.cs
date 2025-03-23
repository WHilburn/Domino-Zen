using UnityEngine;
using System.Collections;

public class Domino : MonoBehaviour
{
    private Rigidbody rb;
    private float stillnessThreshold = 0.05f;  // Velocity threshold to consider "stationary"
    private float checkDelay = 0.4f; // How often to check if it's stationary
    public Vector3 holdPoint;

    void Start()
    {
        checkDelay += Random.Range(0f, 0.2f);
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        StartCoroutine(CheckStillnessRoutine()); // Start the coroutine
    }

    private IEnumerator CheckStillnessRoutine()
    {
        while (true)
        {
            yield return new WaitForSeconds(checkDelay);

            if (rb.velocity.sqrMagnitude < stillnessThreshold * stillnessThreshold ||
                rb.angularVelocity.sqrMagnitude < stillnessThreshold * stillnessThreshold)
            {
                rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
            }
            else
            {
                rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
            }
        }
    }
}


