using UnityEngine;
using System.Collections;

public abstract class DominoLike : MonoBehaviour
{
    public static Vector3 holdPoint = new(0, 0.5f, 0); // Point where the domino is held
    public static Vector3 bottomPoint = new(0, -0.5f, 0); // Default bottom point for snapping

    public virtual void SnapToGround()
    {
        // Temporarily disable physics
        Rigidbody rb = GetComponent<Rigidbody>();
        bool wasKinematic = false;

        if (rb != null)
        {
            wasKinematic = rb.isKinematic;
            rb.isKinematic = true; // Disable physics to prevent interference
        }

        // Perform a raycast from the object's origin in the direction of bottomPoint
        Vector3 rayDirection = transform.TransformDirection(bottomPoint);

        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("EnvironmentLayer")))
        {
            // Snap the object's position to the hit point minus the bottomPoint vector
            transform.position = hitInfo.point - transform.TransformDirection(bottomPoint);
            // If the object has a DebugDomino component, log the new position
        }

        // Restore the kinematic state on the next frame
        if (rb != null)
        {
            StartCoroutine(RestoreKinematic(rb, wasKinematic));
        }
    }

    private IEnumerator RestoreKinematic(Rigidbody rb, bool wasKinematic)
    {
        yield return null; // Wait for the next frame
        rb.isKinematic = wasKinematic; // Restore the original kinematic state
    }

    protected bool CheckAndResolveOverlap()
    {
        Collider[] colliders = Physics.OverlapSphere(transform.position, 0.05f); // Tiny point at the origin
        foreach (Collider collider in colliders)
        {
            if (collider.gameObject != gameObject && collider.GetComponent(GetType()) != null)
            {
                Debug.LogWarning(gameObject.name + " detected an overlap with " + collider.gameObject.name + " at " + collider.transform.position + ". This object will self destruct in 3...2...1...*poof*", this);
                DestroyImmediate(gameObject); // Delete the object
                return true;
            }
        }
        return false;
    }
}