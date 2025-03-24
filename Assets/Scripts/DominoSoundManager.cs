using UnityEngine;
using System.Collections;
using System;

public class DominoSoundManager : MonoBehaviour
{
    public AudioSource cascadeSource; // Background cascade sound
    public AudioClip cascadeClip; // Long, rolling cascade sound
    public int movingDominoes = 0; // Number of currently moving dominoes
    private Coroutine cascadeFadeCoroutine;

    void Start()
    {
        cascadeSource.clip = cascadeClip;
        cascadeSource.loop = true;
        cascadeSource.volume = 0f; // Start silent
        cascadeSource.Play();
    }

    void Update()
    {
        cascadeSource.volume = Mathf.Clamp01((movingDominoes - 25) * 0.04f);
    }

    public void RegisterMovingDomino()
    {
        movingDominoes++;
    }

    public void UnregisterMovingDomino()
    {
        if (movingDominoes > 0)
            movingDominoes--;
    }

    private IEnumerator FadeOutCascade()
    {
        while (cascadeSource.volume > 0)
        {
            cascadeSource.volume -= Time.deltaTime * 0.5f; // Smooth fade out
            yield return null;
        }
        cascadeSource.Stop();
    }
}
