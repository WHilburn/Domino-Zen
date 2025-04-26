using UnityEngine;
using DG.Tweening;
using TMPro; // Import DOTween namespace

public class VictoryAnimation : MonoBehaviour
{
    public GameObject dominoPrefab; // The Domino2D prefab to instantiate
    public Vector2 spawnPos = new Vector2(-7.5f, 0f); // Starting position for the first domino
    public int dominoCount = 20; // Number of dominoes to spawn
    public float spacing = 160f; // Spacing between dominoes on the x-axis
    public Vector2 forceDirection = new Vector2(0f, -1000f); // Direction of the force to apply
    public float scaleUpDuration = 0.25f; // Duration for scaling up the dominoes
    public AudioClip victorySound; // Sound to play on victory
    public TextMeshProUGUI victoryText; // Reference to the TextMeshProUGUI component for the victory text

    private GameObject[] dominoes; // Array to store the spawned dominoes

    void Start()
    {
        // Initialize the dominoes array
        dominoes = new GameObject[dominoCount];

        DominoSoundManager.Instance.PlayArbitrarySound(victorySound, 1f, 1f); // Play the victory sound

        // Spawn the dominoes
        for (int i = 0; i < dominoCount; i++)
        {
            Vector2 position = new Vector2(spawnPos.x + i * spacing, spawnPos.y);
            dominoes[i] = Instantiate(dominoPrefab, position, Quaternion.identity, transform); // Set parent to this GameObject

            // Set initial scale and animate scaling up
            Transform dominoTransform = dominoes[i].transform;
            dominoTransform.localScale = new Vector3(dominoTransform.localScale.x, 0f, dominoTransform.localScale.z);
            dominoTransform.DOScaleY(1f, scaleUpDuration);
        }

        // Apply force to the first domino after the scaling animation is complete
        Invoke(nameof(ApplyForceToFirstDomino), scaleUpDuration);
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
}
