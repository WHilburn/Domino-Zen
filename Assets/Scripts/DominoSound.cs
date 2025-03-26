using UnityEngine;
public class DominoSound : MonoBehaviour
{
    private AudioSource audioSource;
    private Rigidbody rb;
    public DominoSoundList soundList;
    public float minimumImpactForce = 1f;
    public DominoSoundList dominoClickSounds;
    private bool musicMode = false;
    private static int lastPlayedNoteIndex = 0;
    public float soundCooldown = 0.2f; // Minimum time between sounds
    private static float lastSoundTime = 0f;  // Tracks the last time a sound was played


    void Start()
    {
        audioSource = GetComponent<AudioSource>() ?? gameObject.AddComponent<AudioSource>();
        rb = GetComponent<Rigidbody>();

        // If domino sound list is DominoClickSounds, set source pitch to 2
        if (soundList == dominoClickSounds)
        {
            audioSource.pitch = 2;
        }
        else musicMode = true;
    }

    void OnCollisionEnter(Collision collision)
    {
        if (soundList == null || soundList.sounds.Length == 0) return; // Exit if no sounds are assigned

        float impactForce = collision.relativeVelocity.magnitude; // Get collision force

        // Only play a sound if impact is strong enough and cooldown has passed
        if (impactForce >= minimumImpactForce && Time.time >= lastSoundTime + soundCooldown)
        {
            lastSoundTime = Time.time; // Update the last sound time

            AudioClip clip = soundList.sounds[Random.Range(0, soundList.sounds.Length)]; // Choose a random sound from the list
            if (musicMode)
            {
                lastPlayedNoteIndex = (lastPlayedNoteIndex + 1) % soundList.sounds.Length;
                clip = soundList.sounds[lastPlayedNoteIndex];
                Debug.Log("Playing note " + lastPlayedNoteIndex);
            }

            // Adjust volume based on impact force (clamped between 0.1 and 1.0)
            float volume = Mathf.Clamp(impactForce / 10f, 0.1f, 1.0f);
            audioSource.PlayOneShot(clip, volume);

            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;
        }
    }
}