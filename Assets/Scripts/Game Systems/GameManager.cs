using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<PlacementIndicator> allIndicators;  // List of all dominoes in the scene
    private bool physicsEnabled = true; // Whether domino physics are enabled
    public bool debugMode = true; // Debug mode toggle

    public enum GameDifficulty
    {
        Easy,
        Medium,
        Hard
    }
    public GameDifficulty gameDifficulty = GameDifficulty.Easy; // Default difficulty

    void Start()
    {
        DOTween.SetTweensCapacity(20000, 20000);
        DOTween.defaultRecyclable = true; // Enable DOTween recycling
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        foreach (var indicator in FindObjectsOfType<PlacementIndicator>())
        {
            allIndicators.Add(indicator);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }   
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleDominoPhysics();
        }

        if (debugMode)
        {
            for (int i = 0; i <= 9; i++)
            {
                if (Input.GetKeyDown(KeyCode.Alpha0 + i))
                {
                    Time.timeScale = i == 0 ? 1f : i * 0.1f; // Set game speed
                    Debug.Log($"Game speed set to {Time.timeScale}");
                }
            }
        }
    }

    public void CheckCompletion()
    {
        // check if every indicator is satisfied
        bool allIndicatorsFilled = true;
        foreach (var indicator in allIndicators)
        {
            if (indicator.currentState != PlacementIndicator.IndicatorState.Filled)
            {
                allIndicatorsFilled = false;
                break;
            }
        }
        if (allIndicatorsFilled)
        {
            Debug.Log("***********All indicators have been filled!*************");
        }
    }

    private void ToggleDominoPhysics()
    {
        physicsEnabled = !physicsEnabled; // Flip state
        Debug.Log($"Domino physics is now {(physicsEnabled ? "ENABLED" : "DISABLED")}");
        
        // Find all dominoes (assumes they have a "Domino" tag or a common component)
        GameObject[] dominoes = GameObject.FindGameObjectsWithTag("DominoTag");

        foreach (GameObject domino in dominoes)
        {
            Rigidbody rb = domino.GetComponent<Rigidbody>();
            MonoBehaviour[] scripts = domino.GetComponents<MonoBehaviour>();
            BoxCollider collider = domino.GetComponent<BoxCollider>();

            if (rb != null)
            {
                rb.isKinematic = !physicsEnabled; // Toggle physics
                rb.useGravity = physicsEnabled; // Ensure gravity behaves correctly
                collider.enabled = physicsEnabled;
            }

            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = physicsEnabled; // Toggle all scripts
            }
        }
    }
}
