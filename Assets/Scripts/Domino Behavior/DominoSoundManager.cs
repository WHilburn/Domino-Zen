using UnityEngine;
using System.Collections;
using System.Collections.Generic;

public class DominoSoundManager : MonoBehaviour
{
    public AudioSource cascadeSource; // Background cascade sound
    public AudioClip cascadeClip;     // Rolling cascade sound
    public float maxVolume = .7f;   // Max volume when many dominoes are moving
    public float velocityScale = 0.01f; // Scale factor for volume adjustment
    public float volumeLerpSpeed = 2f; // Speed at which volume adjusts
    private float targetVolume = 0f; // The volume we want to reach
    public float totalVelocityThreshhold = -100f; // Minimum velocity to play the sound, total must exceed this
    public float minimumVelocity = 0.7f; // Minimum velocity for a piece to contibute to the cascase sound
    private float totalMovement = 0f; // Total movement of all dominoes

    void Start()
    {
        // Setup cascade audio
        cascadeSource.clip = cascadeClip;
        cascadeSource.loop = true;
        cascadeSource.volume = 0f;
        cascadeSource.Play();
    }

    void Update()
    {
        // Compute the target volume based on movement
        targetVolume = Mathf.Clamp(totalMovement * velocityScale, 0f, maxVolume);
        // Reset total movement for the next interval
        totalMovement = totalVelocityThreshhold;
        // Smoothly interpolate the actual volume toward the target volume
        cascadeSource.volume = Mathf.Lerp(cascadeSource.volume, targetVolume, Time.deltaTime * volumeLerpSpeed);
    }

    public void UpdateDominoMovement(float velocityMagnitude)
    {
        if (velocityMagnitude > minimumVelocity)
        {
            totalMovement += Mathf.Clamp(velocityMagnitude, 0f, 20f);
        }
    }
}