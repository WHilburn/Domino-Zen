using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DominoSoundManager : MonoBehaviour
{
    public AudioSource cascadeSource; // Background cascade sound
    public AudioClip cascadeClip;     // Rolling cascade sound
    public float maxVolume = 0.7f;   // Max volume when many dominoes are moving
    public float velocityScale = 0.04f; // Scale factor for volume adjustment
    public float volumeLerpSpeed = 2f; // Speed at which volume adjusts
    private float targetVolume = 0f; // The volume we want to reach
    public float minimumVelocity = -75f; // Minimum velocity to play the sound, total must exceed this
    private float totalMovement = 0f; // Total movement of all dominoes

    void Start()
    {
        // Setup cascade audio
        cascadeSource.clip = cascadeClip;
        cascadeSource.loop = true;
        cascadeSource.volume = 0f;
        cascadeSource.Play();

        // Start checking motion every 0.1s
        // StartCoroutine(UpdateTargetVolume());
    }

    void Update()
    {
        // Compute the target volume based on movement
        targetVolume = Mathf.Clamp01(totalMovement * velocityScale);
        // Reset total movement for the next interval
        totalMovement = minimumVelocity;
        // Smoothly interpolate the actual volume toward the target volume
        cascadeSource.volume = Mathf.Lerp(cascadeSource.volume, targetVolume, Time.deltaTime * volumeLerpSpeed);
    }

    public void UpdateDominoMovement(float velocityMagnitude)
    {
        totalMovement += Mathf.Clamp(velocityMagnitude, 0f, 20f);
    }
}