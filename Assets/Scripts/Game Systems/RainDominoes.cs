using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Needed for List
using DG.Tweening;

public class DominoRain : MonoBehaviour {
    public List<Sprite> sprites; // List of sprites assigned in the Inspector
    public GameObject dominoPrefab; // Prefab with an Image component
    public Transform canvasTransform; // Reference to the Canvas
    public float rainDuration = 5f; // How long the effect lasts
    public float spawnRate = 0.1f; // Time between spawns
    public float minFallSpeed = 200f; // Speed of falling dominos
    private Vector2 spawnRangeX = new(-500, 500); // X spawn limits
    public float randomForce = 100f;
    public float randomTorque = 100f;
    public GameObject bigDominoPrefab; // Prefab for the big domino
    public float bigDominoSlideDuration = 1f; // Duration for the big domino to slide in/out
    public Transform bigDominoParent; // Parent for the big domino (should be behind the raining dominoes)
    public MainMenuManager mainMenuManager; // Reference to the MainMenuManager script
    private DominoThrobber[] dominoThrobber; // Reference to the DominoThrobber script
    private float elapsedTime = 0f;

    void Start() {
        StartCoroutine(RainDominoes());
        RectTransform canvasRect = canvasTransform.GetComponent<RectTransform>();
        spawnRangeX = new Vector2(-canvasRect.rect.width / 2 - 100, canvasRect.rect.width / 2 + 100);

        // Fade in the AudioSource using DOTween
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null) {
            audioSource.volume = 0f; // Start with volume at 0
            audioSource.Play(); // Start playing the audio
            DOTween.To(() => audioSource.volume, x => audioSource.volume = x, .7f, 3f); // Fade to full volume over 2 seconds
        }

        // Start the big domino transition
        StartCoroutine(BigDominoTransition());

        // Find all DominoThrobber components in the scene
        dominoThrobber = FindObjectsOfType<DominoThrobber>();
    }

    void Awake()
    {
        DontDestroyOnLoad(this.gameObject);
    }

    IEnumerator RainDominoes() {
        while (elapsedTime < rainDuration) {
            SpawnDomino();
            yield return new WaitForSeconds(spawnRate);
            elapsedTime += spawnRate;
        }
    }

    void SpawnDomino() {
        // Create UI Image for the domino
        GameObject domino = Instantiate(dominoPrefab, canvasTransform);
        Image image = domino.GetComponent<Image>();

        // Assign a random sprite from the list
        image.sprite = sprites[Random.Range(0, sprites.Count)];

        // Set random start position
        RectTransform rectTransform = domino.GetComponent<RectTransform>();
        rectTransform.anchoredPosition = new Vector2(
            Random.Range(spawnRangeX.x, spawnRangeX.y),
            Screen.height / 2 + 400 // Spawn slightly above the screen
        );

        // Apply random rotation and color
        rectTransform.rotation = Quaternion.Euler(0, 0, Random.Range(0, 360));
        image.color = new Color(Random.value, Random.value, Random.value, 1f);

        // Add Rigidbody2D for physics-based falling
        Rigidbody2D rb = domino.GetComponent<Rigidbody2D>();
        if (rb != null)
        {
            // Apply random downward force
            float force = Random.Range(randomForce, randomForce * 5);
            rb.AddForce(Vector2.down * force);

            // Apply random torque for spinning
            float torque = Random.Range(-randomTorque, randomTorque);
            rb.AddTorque(torque);
        }

        // Destroy the domino after it falls off-screen
        StartCoroutine(DestroyWhenOffScreen(domino));
    }

    IEnumerator DestroyWhenOffScreen(GameObject domino) {
        RectTransform rectTransform = domino.GetComponent<RectTransform>();
        while (rectTransform.anchoredPosition.y > -Screen.height) {
            yield return null; // Wait for the next frame
        }
        Destroy(domino);
    }

    IEnumerator BigDominoTransition() {
        // Instantiate the big domino
        GameObject bigDomino = Instantiate(bigDominoPrefab, bigDominoParent);
        RectTransform rectTransform = bigDomino.GetComponent<RectTransform>();

        // Set initial position off-screen to the left
        rectTransform.anchoredPosition = new Vector2(-Screen.width, 0);

        // Slide in to the center of the screen
        yield return rectTransform.DOAnchorPos(Vector2.zero, bigDominoSlideDuration).SetEase(Ease.InOutQuad).WaitForCompletion();
        
        
        foreach (DominoThrobber throbber in dominoThrobber) {
            throbber.StopLoop(); // Allow the throbber to set a new stable position
        }
        
        AudioSource audioSource = GetComponent<AudioSource>();
        if (audioSource != null) {
            // Fade out the AudioSource without using DOTween
            StartCoroutine(FadeOutAudio(audioSource, 2f));
        }
        // Trigger the scene transition
        mainMenuManager.CompleteSceneTransitions(); // Call the method to complete scene transitions
        
        // Wait for a short delay (optional)
        foreach (Domino domino in FindObjectsOfType<Domino>()) {
            Destroy(domino.gameObject); // Destroy the dominoes
        }
        yield return new WaitForSeconds(0.5f);
        StopCoroutine(RainDominoes());
        elapsedTime = Mathf.Infinity; // Stop the rain effect

        // Shrink and spin the big domino towards the middle of the screen
        Sequence shrinkAndSpin = DOTween.Sequence();
        shrinkAndSpin.Join(rectTransform.DOScale(Vector3.zero, bigDominoSlideDuration).SetEase(Ease.InOutQuad));
        shrinkAndSpin.Join(rectTransform.DORotate(new Vector3(0, 0, 360 * 4), bigDominoSlideDuration, RotateMode.FastBeyond360));
        yield return shrinkAndSpin.WaitForCompletion();

        // Destroy the big domino
        Destroy(bigDomino);
    }

    IEnumerator FadeOutAudio(AudioSource audioSource, float duration) {
        float startVolume = audioSource.volume;
        for (float t = 0; t < duration; t += Time.deltaTime) {
            audioSource.volume = Mathf.Lerp(startVolume, 0f, t / duration);
            yield return null;
        }
        audioSource.Stop(); // Stop the audio after fading out
    }
}