using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<PlacementIndicator> allIndicators;  // List of all dominoes in the scene
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
}
