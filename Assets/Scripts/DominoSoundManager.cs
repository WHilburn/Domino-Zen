using UnityEngine;
using System.Collections;

public class DominoSoundManager : MonoBehaviour
{
    public AudioSource cascadeSource; // Background cascade sound
    public AudioClip cascadeClip;     // Rolling cascade sound
    private Domino[] allDominoes;     // List of all dominoes in scene

    public float maxVolume = 0.7f;   // Max volume when many dominoes are moving
    public float velocityScale = 0.05f; // Scale factor for volume adjustment
    public float volumeLerpSpeed = 2f; // Speed at which volume adjusts
    private float targetVolume = 0f; // The volume we want to reach
    public float minimumVelocity = -100f; // Minimum velocity to play the sound

    void Start()
    {
        // Find all dominoes in the scene
        allDominoes = FindObjectsOfType<Domino>();

        // Setup cascade audio
        cascadeSource.clip = cascadeClip;
        cascadeSource.loop = true;
        cascadeSource.volume = 0f;
        cascadeSource.Play();

        // Start checking motion every 0.1s
        StartCoroutine(UpdateTargetVolume());
    }

    void Update()
    {
        // Smoothly interpolate the actual volume toward the target volume
        cascadeSource.volume = Mathf.Lerp(cascadeSource.volume, targetVolume, Time.deltaTime * volumeLerpSpeed);
    }

    public IEnumerator UpdateTargetVolume()
    {
        while (true)
        {
            float totalMovement = minimumVelocity;

            // Sum up velocity and rotational velocity for all dominoes
            foreach (var domino in allDominoes)
            {
                if (domino != null)
                {
                    Rigidbody rb = domino.GetComponent<Rigidbody>();
                    if (rb != null)
                    {
                        totalMovement += rb.velocity.magnitude + rb.angularVelocity.magnitude;
                    }
                }
            }

            // Compute the target volume based on movement
            targetVolume = Mathf.Clamp01(totalMovement * velocityScale);

            yield return new WaitForSeconds(0.1f); // Update movement calculation every 0.1s
        }
    }
}
