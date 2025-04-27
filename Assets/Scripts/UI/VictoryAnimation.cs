using UnityEngine;
using DG.Tweening;
using TMPro;
using UnityEngine.UI;

public class VictoryAnimation : MonoBehaviour
{
    public GameObject dominoPrefab; // The Domino2D prefab to instantiate
    public GameObject TextPrefab; // The TextMeshPro prefab to instantiate
    public GameObject victoryMenu; // The Victory Menu object
    private GameObject victoryTextObject; // Reference to the instantiated text object
    public TextMeshProUGUI timeElapsedText;
    public TextMeshProUGUI resetsText;
    public Vector2 spawnPos = new Vector2(-7.5f, 0f); // Starting position for the first domino
    public int dominoCount = 20; // Number of dominoes to spawn
    public float spacing = 160f; // Spacing between dominoes on the x-axis
    public Vector2 forceDirection = new Vector2(0f, -1000f); // Direction of the force to apply
    public float scaleUpDuration = 0.25f; // Duration for scaling up the dominoes
    public AudioClip victorySound; // Sound to play on victory
    private GameObject[] dominoes; // Array to store the spawned dominoes
    private bool isVictoryAnimationTriggered = false; // Flag to check if the victory animation has been triggered once

    void Start()
    {
        DominoResetManager.OnDominoesStoppedFalling.AddListener(HandleDominoesStoppedFalling); // Subscribe to OnResetEnd event
        // TriggerVictoryAnimation();
    }

    private void OnDestroy()
    {
        DominoResetManager.OnDominoesStoppedFalling.RemoveListener(HandleDominoesStoppedFalling); // Unsubscribe to avoid memory leaks
    }

    public void TriggerVictoryAnimation()
    {
        // Delete existing dominoes if they exist
        if (dominoes != null)
        {
            foreach (GameObject domino in dominoes)
            {
                Destroy(domino);
            }
        }
        // Delete existing victory text if it exists
        if (victoryTextObject != null)
        {
            Destroy(victoryTextObject);
        }
        victoryMenu.SetActive(false); // Hide the victory menu

        // Initialize the dominoes array
        dominoes = new GameObject[dominoCount];
        if (!isVictoryAnimationTriggered)
        {
            DominoSoundManager.Instance.PlayArbitrarySound(victorySound, .5f, 1f); // Play the victory sound if this is the first time
        }

        // Spawn the dominoes
        for (int i = 0; i < dominoCount; i++)
        {
            Vector2 position = new Vector2(spawnPos.x + i * spacing, spawnPos.y);
            dominoes[i] = Instantiate(dominoPrefab, position, Quaternion.identity, transform); // Set parent to this GameObject

            // Set initial scale and animate scaling up
            Transform dominoTransform = dominoes[i].transform;
            dominoTransform.localScale = new Vector3(dominoTransform.localScale.x, 0f, dominoTransform.localScale.z);
            dominoTransform.DOScaleY(1f, scaleUpDuration);

            // Assign a pastel rainbow color
            Image image = dominoes[i].GetComponent<Image>();
            if (image != null)
            {
                float hue = (float)i / (dominoCount/1.5f); // Calculate hue based on position in the array
                Color pastelColor = Color.HSVToRGB(hue, 0.5f, 1f); // Pastel colors have lower saturation
                image.color = pastelColor;
            }
        }

        // Apply force to the first domino after the scaling animation is complete
        Invoke(nameof(ApplyForceToFirstDomino), scaleUpDuration);

        // Tween the victory text after 1 second
        Invoke(nameof(ShowVictoryText), 1f);
        Invoke(nameof(DisablePhysics), dominoCount * .125f); // Disable physics after 1 second
        Invoke(nameof(ShowVictoryMenu), 4f); // Show victory menu after 5 seconds
        isVictoryAnimationTriggered = true; // Set the flag to true
    }

    public void TriggerDominoReset()
    {
        // Hide text, dominoes, and menu
        foreach (GameObject domino in dominoes)
        {
            if (domino != null)
            {
                domino.SetActive(false);
            }
        }

        if (victoryTextObject != null)
        {
            victoryTextObject.SetActive(false);
        }

        victoryMenu.SetActive(false);

        // Trigger the domino reset
        DominoResetManager.Instance.ResetAllDominoes();
    }

    private void HandleDominoesStoppedFalling()
    {
        if (!GameManager.levelComplete) return;

        // Check if dominoes array is null or empty
        if (dominoes == null || dominoes.Length == 0) return;

        // Reappear text, dominoes, and menu
        foreach (GameObject domino in dominoes)
        {
            if (domino != null)
            {
                domino.SetActive(true);
            }
        }

        if (victoryTextObject != null)
        {
            victoryTextObject.SetActive(true);
        }

        victoryMenu.SetActive(true);
    }

    private void ShowVictoryText()
    {
        // Instantiate the text prefab as a child of this object
        victoryTextObject = Instantiate(TextPrefab, transform);
        TextMeshProUGUI victoryText = victoryTextObject.GetComponent<TextMeshProUGUI>();

        if (victoryText != null)
        {
            victoryTextObject.transform.localPosition = Vector3.zero; // Position it relative to the parent
            victoryTextObject.transform.localScale = new Vector3(5f, 0f, 1f); // Start with wide and flat scale
            victoryTextObject.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f); // Tween to normal size
        }

        // Update timeElapsedText and resetsText
        if (timeElapsedText != null)
        {
            float elapsedTime = GameManager.elapsedTime;
            int hours = Mathf.FloorToInt(elapsedTime / 3600);
            int minutes = Mathf.FloorToInt((elapsedTime % 3600) / 60);
            int seconds = Mathf.FloorToInt(elapsedTime % 60);
            timeElapsedText.text = $"Time Elapsed:\n {hours:D2}:{minutes:D2}:{seconds:D2}";
        }

        if (resetsText != null)
        {
            resetsText.text = $"Resets:\n {GameManager.resetsTriggered}";
        }
    }

    void Update()
    {
        // Trigger the force application when the V key is pressed
        if (Input.GetKeyDown(KeyCode.V))
        {
            ApplyForceToFirstDomino();
        }
    }

    private void ApplyForceToFirstDomino()
    {
        // Apply a force to the first domino to knock it over
        if (dominoes.Length > 0)
        {
            Rigidbody2D rb = dominoes[0].GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                // Calculate the top position of the domino
                Vector2 topPosition = rb.transform.position + new Vector3(0f, rb.GetComponent<Collider2D>().bounds.extents.y, 0f);
                rb.AddForceAtPosition(forceDirection, topPosition, ForceMode2D.Impulse); // Apply a downward force at the top
            }
        }
    }

    public void DisablePhysics()
    {
        // Disable physics for all dominoes
        foreach (GameObject domino in dominoes)
        {
            Rigidbody2D rb = domino.GetComponent<Rigidbody2D>();
            if (rb != null)
            {
                rb.simulated = false; // Disable physics simulation
            }
        }
    }

    private void ShowVictoryMenu()
    {
        // Tween dominoes and text upwards
        foreach (GameObject domino in dominoes)
        {
            if (domino != null)
            {
                domino.transform.DOMoveY(domino.transform.position.y + 200f, 1f); // Move dominoes upwards
            }
        }

        Transform victoryTextTransform = transform.Find(TextPrefab.name + "(Clone)");
        if (victoryTextTransform != null)
        {
            victoryTextTransform.DOMoveY(victoryTextTransform.position.y + 200f, 1f); // Move text upwards
        }

        // Animate the victory menu
        victoryMenu.SetActive(true); // Activate the victory menu
        RectTransform victoryMenuTransform = victoryMenu.GetComponent<RectTransform>();
        if (victoryMenuTransform != null)
        {
            victoryMenuTransform.anchoredPosition = new Vector2(0f, -Screen.height / 2f); // Start at the bottom center
            victoryMenuTransform.DOAnchorPos(new Vector2(0f, -Screen.height / 4f), 1f); // Slide up to the lower center
        }
        victoryMenu.transform.localScale = new Vector3(5f, 0f, 1f); // Start with wide and flat scale
        victoryMenu.transform.DOScale(new Vector3(1f, 1f, 1f), 0.5f); // Tween to normal size after sliding
    }
}
