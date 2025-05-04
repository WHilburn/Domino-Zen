using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System;

public class GameManager : MonoBehaviour
{
    public static GameManager Instance { get; private set; }
    public HashSet<PlacementIndicator> allIndicators = new HashSet<PlacementIndicator>();  // Set of all dominoes in the scene
    public bool debugMode = true; // Debug mode toggle
    public static bool levelComplete = false; // Flag to indicate if the level is complete
    public static bool gamePaused = false; // Flag to indicate if the game is paused
    public Camera mainCamera; // Reference to the main camera
    public VictoryAnimation victoryAnimation; // Reference to the VictoryAnimation script
    public static float elapsedTime = 0f; // Time elapsed since the level started
    public static int resetsTriggered = 0;
    private bool isTiming = false; // Flag to track if the timer is running
    public GameObject originatorDomino; // Reference to the domino or placement indicator at the start of the chain in each level
    public GameObject levelCompletePopup;
    public static UnityEvent OnLevelComplete = new UnityEvent(); // Event triggered when the level is completed
    public static int filledIndicators = 0; // Number of filled indicators in the scene
    private Bucket bucket; // Reference to the Bucket script
    public GameObject dominoPrefab; // Prefab for the domino

    [SerializeField]
    private GameDifficulty editorGameDifficulty = GameDifficulty.Relaxed; // Backing field for editor

    public enum GameDifficulty
    {
        Relaxed,
        Focused,
        Intense
    }
    public static GameDifficulty gameDifficulty = GameDifficulty.Relaxed; // Default difficulty
    public static UnityEvent<GameDifficulty> OnGameDifficultyChanged = new UnityEvent<GameDifficulty>();

    [Serializable]
    public class LevelData
    {
        public string levelName; // Name of the level
        public string sceneName; // Unity scene associated with the level
        [TextArea] public string description; // Description text for the level
        public Sprite levelImage; // Image associated with the level
    }

    [System.Serializable]
    public class LevelStats
    {
        public float bestTime = float.MaxValue; // Best completion time (in seconds)
        public GameDifficulty hardestDifficulty = GameDifficulty.Relaxed; // Hardest difficulty completed
        public bool isInProgress = false; // Whether the level is currently in progress
        public float inProgressTime = 0f; // Time elapsed for the current in-progress attempt
    }

    public static Dictionary<string, LevelStats> levelStats = new Dictionary<string, LevelStats>(); // Store stats for each level

    public void SaveLevelStats()
    {
        foreach (var level in levels)
        {
            if (levelStats.TryGetValue(level.sceneName, out var stats))
            {
                PlayerPrefs.SetFloat(level.sceneName + "_BestTime", stats.bestTime);
                PlayerPrefs.SetInt(level.sceneName + "_HardestDifficulty", (int)stats.hardestDifficulty);
            }
        }
        PlayerPrefs.Save();
    }

    public LevelStats LoadLevelStats(string levelName)
    {
        var stats = new LevelStats();

        if (PlayerPrefs.HasKey(levelName + "_BestTime") && PlayerPrefs.HasKey(levelName + "_HardestDifficulty"))
        {
            stats.bestTime = PlayerPrefs.GetFloat(levelName + "_BestTime", float.MaxValue);
            stats.hardestDifficulty = (GameDifficulty)PlayerPrefs.GetInt(levelName + "_HardestDifficulty", (int)GameDifficulty.Relaxed);
        }
        else
        {
            Debug.LogWarning($"No saved stats found for level: {levelName}. Using default values.");
            stats.bestTime = float.MaxValue;
            stats.hardestDifficulty = GameDifficulty.Relaxed;
        }

        levelStats[levelName] = stats;
        return stats;
    }

    public List<LevelData> levels = new List<LevelData>(); // List of levels, editable in the Unity Editor

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
        bucket = FindObjectOfType<Bucket>(); // Find the Bucket script in the scene
        if (bucket == null)
        {
            if (SceneManager.GetActiveScene().name != "Main Menu")
            Debug.LogWarning("Bucket not found in the scene!");
        }
        else bucket.gameObject.SetActive(false);
        DominoResetManager.OnDominoesStoppedFalling.AddListener(OnDominoesStoppedFalling);
        gameDifficulty = editorGameDifficulty; // Synchronize static field with editor value
        if (SceneManager.GetActiveScene().name != "Main Menu")
        LevelProgressManager.LoadProgress(SceneManager.GetActiveScene().name, allIndicators); // Load progress at the start of the level
    }

    void OnDestroy()
    {
        if (SceneManager.GetActiveScene().name != "Main Menu")
        {
            SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from sceneLoaded event
            DominoResetManager.OnResetStart.RemoveListener(OnResetStartHandler); // Unsubscribe from OnResetStart event
            DominoResetManager.OnDominoesStoppedFalling.RemoveListener(OnDominoesStoppedFalling);
            LevelProgressManager.SaveProgress(SceneManager.GetActiveScene().name, GetFilledIndicators()); // Save progress when the level is destroyed
        }
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebuildAllIndicators(); // Rebuild the list when a new scene is loaded
    }

    private void RebuildAllIndicators()
    {
        allIndicators.Clear();;
        foreach (var indicator in FindObjectsOfType<PlacementIndicator>())
        {
            allIndicators.Add(indicator);
        }
        // Debug.Log($"Rebuilt allIndicators list with {allIndicators.Count} indicators.");
    }

    void Update()
    {
        if (debugMode)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                SceneManager.LoadScene(SceneManager.GetActiveScene().name);
            }
            if (Input.GetKeyDown(KeyCode.K))
            {
                Debug.Log("K key pressed - triggering level completion.");
                CheckCompletion();
            }
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
        filledIndicators = 0;
        foreach (var indicator in allIndicators)
        {
            if (indicator.currentState == PlacementIndicator.IndicatorState.Filled)
            {
                filledIndicators++;
            }
        }
        Debug.Log($"Filled indicators: {filledIndicators} / {allIndicators.Count}");
        if (filledIndicators == allIndicators.Count || Input.GetKeyDown(KeyCode.K))
        {
            Debug.Log("*** All indicators have been filled! ***");
            if (!levelComplete)
            {
                isTiming = false; // Stop the timer
                Debug.Log($"Level completed in {elapsedTime} seconds.");
                levelCompletePopup.SetActive(true); // Show the level complete popup
                RecordLevelStats(); // Record level stats
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
        HandleBucketState();
    }

    public void HandleBucketState()
    {
        if (gameDifficulty == GameDifficulty.Intense)
        {
            if (bucket != null)
            {
                bucket.gameObject.SetActive(true);
                PlayerDominoPlacement.Instance.bucketModeEnabled = true;
            }
        }
        else
        {
            if (bucket != null)
            {
                bucket.gameObject.SetActive(false);
                PlayerDominoPlacement.Instance.bucketModeEnabled = false;
            }
        }
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

    public void RecordLevelStats()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (!levelStats.TryGetValue(currentSceneName, out var stats))
        {
            stats = new LevelStats();
            levelStats[currentSceneName] = stats;
        }

        // Update best time if the current time is better
        if (elapsedTime < stats.bestTime)
        {
            stats.bestTime = elapsedTime;
        }

        // Update hardest difficulty if the current difficulty is harder
        if (gameDifficulty > stats.hardestDifficulty)
        {
            stats.hardestDifficulty = gameDifficulty;
        }

        stats.isInProgress = false; // Mark the level as not in progress

        Debug.Log($"Level stats updated: Best Time = {stats.bestTime}, Hardest Difficulty = {stats.hardestDifficulty}");

        // Save the updated stats
        SaveLevelStats();
    }

    private HashSet<PlacementIndicator> GetFilledIndicators()
    {
        var filledIndicators = new HashSet<PlacementIndicator>();
        foreach (var indicator in allIndicators)
        {
            if (indicator.currentState == PlacementIndicator.IndicatorState.Filled)
            {
                filledIndicators.Add(indicator);
            }
        }
        return filledIndicators;
    }
}
