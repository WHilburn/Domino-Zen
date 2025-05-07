using System.Collections.Generic;
using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;
using UnityEngine.SceneManagement;
using System;
using System.Collections;

public class GameManager : MonoBehaviour
{
    #region Singleton
    public static GameManager Instance { get; private set; }
    #endregion

    #region Public Fields
    public HashSet<PlacementIndicator> allIndicators = new HashSet<PlacementIndicator>();
    public bool debugMode = true;
    public static bool levelComplete = false;
    public static bool gamePaused = false;
    public Camera mainCamera;
    public VictoryAnimation victoryAnimation;
    public static float elapsedTime = 0f;
    public static int resetsTriggered = 0;
    public GameObject originatorDomino;
    public GameObject levelCompletePopup;
    public static UnityEvent OnLevelComplete = new UnityEvent();
    public static int filledIndicators = 0;
    public GameObject dominoPrefab;
    public List<LevelData> levels = new List<LevelData>();
    #endregion

    #region Private Fields
    private bool isTiming = false;
    private Bucket bucket;
    [SerializeField]
    private GameDifficulty editorGameDifficulty = GameDifficulty.Relaxed;
    #endregion

    #region Enums
    public enum GameDifficulty
    {
        Relaxed,
        Focused,
        Intense
    }
    public static GameDifficulty gameDifficulty = GameDifficulty.Relaxed;
    public static UnityEvent<GameDifficulty> OnGameDifficultyChanged = new UnityEvent<GameDifficulty>();
    #endregion

    #region Nested Classes
    [Serializable]
    public class LevelData
    {
        public string levelName;
        public string sceneName;
        [TextArea] public string description;
        public Sprite levelImage;
    }

    [System.Serializable]
    public class LevelStats
    {
        public float bestTime = float.MaxValue;
        public GameDifficulty hardestDifficulty = GameDifficulty.Relaxed;
        public bool isInProgress = false;
        public float inProgressTime = 0f;
    }
    #endregion

    #region Static Fields
    public static Dictionary<string, LevelStats> levelStats = new Dictionary<string, LevelStats>();
    #endregion

    #region Unity Lifecycle
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
        StartCoroutine(LoadProgressCoroutine()); // Load progress at the start of the level
    }

    public void OnDestroy()
    {
        SceneManager.sceneLoaded -= OnSceneLoaded; // Unsubscribe from sceneLoaded event
        DominoResetManager.OnResetStart.RemoveListener(OnResetStartHandler); // Unsubscribe from OnResetStart event
        DominoResetManager.OnDominoesStoppedFalling.RemoveListener(OnDominoesStoppedFalling);
    }
    #endregion

    #region Scene Management
    private IEnumerator LoadProgressCoroutine()
    {
        while (SceneLoader.asyncLoad != null)
        {
            yield return null; // Wait for the async load to complete
        }
        yield return new WaitForSeconds(0.05f); // Wait for a short duration to ensure the scene is fully loaded
        if (SceneManager.GetActiveScene().name != "Main Menu" && 
            SceneManager.GetActiveScene().name != "Testing Level" && 
            SceneManager.GetActiveScene().name != "Tutorial Level")
            LevelProgressManager.LoadProgress(SceneManager.GetActiveScene().name, allIndicators); // Load progress at the start of the level
    }

    void OnSceneLoaded(Scene scene, LoadSceneMode mode)
    {
        RebuildAllIndicators(); // Rebuild the list when a new scene is loaded
    }
    #endregion

    #region Indicator Management
    private void RebuildAllIndicators()
    {
        allIndicators.Clear();;
        foreach (var indicator in FindObjectsOfType<PlacementIndicator>())
        {
            allIndicators.Add(indicator);
        }
    }

    public HashSet<PlacementIndicator> GetFilledIndicators()
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
    #endregion

    #region Game State Management
    void Update()
    {
        if (debugMode)
        {
            if (Input.GetKeyDown(KeyCode.T))
            {
                ResetLevel();
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

    public void ResetLevel()
    {
        LevelProgressManager.ResetProgress();
        SceneLoader.Instance.StartSceneTransitionCoroutine(SceneManager.GetActiveScene().name, true);
        gamePaused = false; // Reset the game paused state
        InGameUI.Instance.TogglePauseMenu(false); // Close the pause menu if it's open
    }

    public void CheckCompletion()
    {
        filledIndicators = 0;
        foreach (var indicator in allIndicators)
        {
            if (indicator.currentState == PlacementIndicator.IndicatorState.Filled)
            {
                filledIndicators++;
            }
        }
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
    #endregion

    #region Event Handlers
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
    #endregion

    #region Level Stats
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
            stats.bestTime = float.MaxValue;
            stats.hardestDifficulty = GameDifficulty.Relaxed;
        }

        levelStats[levelName] = stats;
        return stats;
    }

    public void RecordLevelStats()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;

        if (!levelStats.TryGetValue(currentSceneName, out var stats))
        {
            stats = new LevelStats();
            levelStats[currentSceneName] = stats;
        }

        if (elapsedTime < stats.bestTime)
        {
            stats.bestTime = elapsedTime;
        }

        if (gameDifficulty > stats.hardestDifficulty)
        {
            stats.hardestDifficulty = gameDifficulty;
        }

        stats.isInProgress = false;

        Debug.Log($"Level stats updated: Best Time = {stats.bestTime}, Hardest Difficulty = {stats.hardestDifficulty}");

        SaveLevelStats();
    }
    #endregion
}
