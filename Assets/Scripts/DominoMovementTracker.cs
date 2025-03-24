using UnityEngine;

public class DominoMovementTracker : MonoBehaviour
{
    private Rigidbody rb;
    private DominoSoundManager soundManager;
    public float velocityMagnitude;

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        soundManager = FindObjectOfType<DominoSoundManager>();
    }

    void Update()
    {
        if (rb != null && soundManager != null)
        {
            velocityMagnitude = rb.angularVelocity.magnitude;
            soundManager.UpdateDominoMovement(velocityMagnitude);
        }
    }
}