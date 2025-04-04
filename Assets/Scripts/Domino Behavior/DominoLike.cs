using UnityEngine;

public abstract class DominoLike : MonoBehaviour
{
    public static Vector3 holdPoint = new Vector3(0, 0.5f, 0); // Point where the domino is held
    public static Vector3 bottomPoint = new Vector3(0, -0.5f, 0); // Default bottom point for snapping
    public virtual void SnapToGround()
    {
        // Perform a raycast from the object's origin in the direction of bottomPoint
        Vector3 rayDirection = transform.TransformDirection(bottomPoint);
        Debug.DrawLine(transform.position, transform.position + rayDirection * 50, Color.red, 5.0f); // Draw debug line

        if (Physics.Raycast(transform.position, rayDirection, out RaycastHit hitInfo, Mathf.Infinity, LayerMask.GetMask("EnvironmentLayer")))
        {
            // Snap the object's position to the hit point minus the bottomPoint vector
            transform.position = hitInfo.point - transform.TransformDirection(bottomPoint);
            Debug.Log($"Snapped to ground at: {transform.position}"); // Log the new position
        }
    }
}