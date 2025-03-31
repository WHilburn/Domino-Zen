using UnityEngine;

public class DominoSound : MonoBehaviour
{
    private Rigidbody rb;
    private AudioSource audioSource;
    public bool musicMode = true;

    private void Start()
    {
        rb = GetComponent<Rigidbody>();
        audioSource = GetComponent<AudioSource>();
    }

    private void OnCollisionEnter(Collision collision)
    {
        float impactForce = collision.relativeVelocity.magnitude;
        DominoSoundManager.Instance.PlayDominoSound(impactForce, musicMode, audioSource);
        //Set collision to continuous dynamic
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
    }
}