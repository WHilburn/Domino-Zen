using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using System.Collections.Generic; // Needed for List

public class DominoRain : MonoBehaviour {
    public List<Sprite> sprites; // List of sprites assigned in the Inspector
    public GameObject dominoPrefab; // Prefab with an Image component
    public Transform canvasTransform; // Reference to the Canvas
    public float rainDuration = 5f; // How long the effect lasts
    public float spawnRate = 0.1f; // Time between spawns
    public float fallSpeed = 200f; // Speed of falling dominos
    public Vector2 spawnRangeX = new Vector2(-500, 500); // X spawn limits
    public float randomForce = 100f;
    public float randomTorque = 100f;

    private float elapsedTime = 0f;

    void Start() {
        StartCoroutine(RainDominoes());
        RectTransform canvasRect = canvasTransform.GetComponent<RectTransform>();
        spawnRangeX = new Vector2(-canvasRect.rect.width / 2, canvasRect.rect.width / 2);
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
            Screen.height / 2 + 200 // Spawn slightly above the screen
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
        while (rectTransform.anchoredPosition.y > -Screen.height / 1.5) {
            yield return null; // Wait for the next frame
        }
        Destroy(domino);
    }
}