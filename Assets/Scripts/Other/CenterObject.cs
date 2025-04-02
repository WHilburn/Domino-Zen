using UnityEngine;

public class CenterObject : MonoBehaviour
{
    void Start()
    {
        Renderer rend = GetComponent<Renderer>();
        if (rend != null)
        {
            Vector3 center = rend.bounds.center;
            transform.position -= center; // Moves the object so the center is at (0,0,0)
        }
    }
}
