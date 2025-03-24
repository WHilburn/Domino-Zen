using UnityEngine;

public class DominoSound : MonoBehaviour
{
    public AudioClip[] dominoSounds;
    private AudioSource audioSource;
    private Rigidbody rb;

    void Start()
    {
        audioSource = GetComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();
        if (audioSource == null)
        {
            audioSource = gameObject.AddComponent<AudioSource>();
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (dominoSounds.Length == 0) return; // Exit if no sounds are assigned

        float impactForce = collision.relativeVelocity.magnitude; // Get collision force

        // Only play a sound if impact is strong enough
        if (impactForce > 0.2f)
        {
            // Select a random sound
            AudioClip clip = dominoSounds[Random.Range(0, dominoSounds.Length)];
            // Adjust volume based on impact force (clamped between 0.1 and 1.0)
            float volume = Mathf.Clamp(impactForce / 10f, 0.1f, 1.0f);
            audioSource.PlayOneShot(clip, volume);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
}
