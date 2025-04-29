using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.SceneManagement;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public List<PlacementIndicator> allIndicators;  // List of all dominoes in the scene
    public bool debugMode = true; // Debug mode toggle
    public static bool levelComplete = false; // Flag to indicate if the level is complete
    public static bool gamePaused = false; // Flag to indicate if the game is paused
    public VictoryAnimation victoryAnimation; // Reference to the VictoryAnimation script
    public static float elapsedTime = 0f; // Time elapsed since the level started
    public static int resetsTriggered = 0;
    private bool isTiming = false; // Flag to track if the timer is running
    public GameObject originatorDomino; // Reference to the domino or placement indicator at the start of the chain in each level
    public GameObject levelCompletePopup;
    public static UnityEvent OnLevelComplete = new UnityEvent(); // Event triggered when the level is completed

    [SerializeField]
    private GameDifficulty editorGameDifficulty = GameDifficulty.Easy; // Backing field for editor

    public enum GameDifficulty
    {
        Easy,
        Medium,
        Hard
    }
    public static GameDifficulty gameDifficulty = GameDifficulty.Easy; // Default difficulty
    public static UnityEvent<GameDifficulty> OnGameDifficultyChanged = new UnityEvent<GameDifficulty>();

    void Start()
    {
        DOTween.SetTweensCapacity(20000, 20000);
        DOTween.defaultRecyclable = true; // Enable DOTween recycling
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        SceneManager.sceneLoaded += OnSceneLoaded; // Subscribe to sceneLoaded event
        DominoResetManager.OnResetStart.AddListener(OnResetStartHandler); // Subscribe to OnResetStart event
        RebuildAllIndicators(); // Initial rebuild
        elapsedTime = 0f; // Reset elapsed time at the start of the level
        isTiming = true; // Start the timer
        DominoResetManager.OnDominoesStoppedFalling.AddListener(OnDominoesStoppedFalling);
        gameDifficulty = editorGameDifficulty; // Synchronize static field with editor value
    }

    void OnDestroy()
    {
        if (Instance == this)
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from sceneLoaded event
            DominoResetManager.OnResetStart.RemoveListener(OnResetStartHandler); // Unsubscribe from OnResetStart event
            DominoResetManager.OnDominoesStoppedFalling.RemoveListener(OnDominoesStoppedFalling);
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebuildAllIndicators(); // Rebuild the list when a new scene is loaded
    }

    private void RebuildAllIndicators()
    {
        allIndicators.Clear();
        foreach (var indicator in FindObjectsOfType<PlacementIndicator>())
        {
            allIndicators.Add(indicator);
        }
        Debug.Log($"Rebuilt allIndicators list with {allIndicators.Count} indicators.");
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.T))
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
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

        if (isTiming && !gamePaused && !levelComplete)
        {
            elapsedTime += Time.deltaTime; // Increment elapsed time
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
            Debug.Log("*** All indicators have been filled! ***");
            if (!levelComplete)
            {
                isTiming = false; // Stop the timer
                Debug.Log($"Level completed in {elapsedTime} seconds.");
                levelCompletePopup.SetActive(true); // Show the level complete popup
            }
            else
            {
                Debug.Log("Level already completed.");
            }
            levelComplete = true;
            OnLevelComplete.Invoke();
        }
    }

    public void SetGameDifficulty(GameDifficulty newDifficulty)
    {
        gameDifficulty = newDifficulty;
        editorGameDifficulty = newDifficulty; // Update editor field
        Debug.Log($"Game difficulty set to {gameDifficulty}");
        OnGameDifficultyChanged.Invoke(newDifficulty); // Trigger the event
    }

    private void OnResetStartHandler()
    {
        if (!levelComplete)
        {
            resetsTriggered++; // Increment resetsTriggered if the level is not complete
        }
    }

    private void OnDominoesStoppedFalling()
    {
        if (levelComplete)
        {
            victoryAnimation.TriggerVictoryAnimation();
            Debug.Log("Victory animation triggered!");
        }
    }
}
