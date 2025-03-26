using UnityEngine;

public class DominoSound : MonoBehaviour
{
    private AudioSource audioSource;
    private Rigidbody rb;
    public DominoSoundList soundList;
    public DominoSoundList dominoClickSounds;

    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        // If domino sound list is DominoClickSounds, set source pitch to 2
        if (soundList == dominoClickSounds)
        {
            audioSource.pitch = 2;
        }
    }

    void OnCollisionEnter(Collision collision)
    {
        if (soundList == null || soundList.sounds.Length == 0) return; // Exit if no sounds are assigned

        float impactForce = collision.relativeVelocity.magnitude; // Get collision force

        // Only play a sound if impact is strong enough
        if (impactForce > 0.2f)
        {   
            AudioClip clip = soundList.sounds[Random.Range(0, soundList.sounds.Length)];// Choose a random sound from the list

            // Adjust volume based on impact force (clamped between 0.1 and 1.0)
            float volume = Mathf.Clamp(impactForce / 10f, 0.1f, 1.0f);
            audioSource.PlayOneShot(clip, volume);
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
}
