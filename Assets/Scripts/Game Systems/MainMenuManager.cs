using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;

public class MainMenuManager : MonoBehaviour
{
    public TextMeshProUGUI loadingText;
    public Button beginLevelButton;
    public GameObject throbber;
    public float minimumLoadingTime = 1f; // Minimum loading time in seconds

    // Reference to the main camera and virtual cameras
    public Camera mainCamera;
    public CinemachineVirtualCamera optionsMenuCamera;
    public CinemachineVirtualCamera levelSelectCamera;
    public CinemachineVirtualCamera mainMenuCamera;

    public Image circularProgressBar; // Reference to the circular progress bar image
    public TextMeshProUGUI levelDescription; // Text to display the level description
    public Image levelPreviewImage; // Reference to the large preview image for the selected level
    
    // Store current active camera
    private CinemachineVirtualCamera activeCamera;
    public DominoRain dominoRain; // Reference to the DominoRain script for scene transitions
    public GameObject levelSelectButtonPrefab; // Prefab for level select buttons
    public Transform levelSelectScrollViewContent; // Content transform of the scroll view

    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    private List<GameManager.LevelData> levels = new List<GameManager.LevelData>(); // List of levels, editable in the Unity Editor
    private GameManager.LevelData selectedLevel; // Currently selected level

    private void Start()
    {
        // Set the main menu camera as the default
        SetActiveCamera(mainMenuCamera);
        DOTween.defaultRecyclable = true;

        Invoke(nameof(DelayedStart), 0.1f); // Delay the start to allow for camera blending
    }

    private void DelayedStart()
    {
        levels = GameManager.Instance.levels; // Get the list of levels from GameManager
        PopulateLevelSelectButtons(); // Create level select buttons
        InitializeDifficultyButtons();
        SetSelectedLevel(levels.Count > 0 ? levels[0] : null); // Set the default selected level
        UpdateLevelPreviewImage(); // Update the preview image for the default selected level
    }

    public void SetSelectedLevel(GameManager.LevelData level)
    {
        selectedLevel = level;
        beginLevelButton.interactable = level != null; // Enable the button when a level is selected

        // Update the level description text
        if (levelDescription != null && level != null)
        {
            levelDescription.text = $"{level.levelName}\n<size=12>{level.description}</size>";
        }

        UpdateLevelPreviewImage(); // Update the preview image when a new level is selected

        // Force easy difficulty for the tutorial level (if applicable)
        if (level != null && level.levelName == "Tutorial")
        {
            SetGameDifficulty(GameManager.GameDifficulty.Relaxed);
            mediumButton.gameObject.SetActive(false);
            hardButton.gameObject.SetActive(false);
        }
        else
        {
            mediumButton.gameObject.SetActive(true);
            hardButton.gameObject.SetActive(true);
        }
    }

    private void UpdateLevelPreviewImage()
    {
        if (levelPreviewImage != null && selectedLevel != null)
        {
            levelPreviewImage.sprite = selectedLevel.levelImage; // Set the preview image to the selected level's image
            levelPreviewImage.enabled = selectedLevel.levelImage != null; // Enable or disable the image based on its availability
        }
    }

    private void InitializeDifficultyButtons()
    {
        easyButton.onClick.AddListener(() => SetGameDifficulty(GameManager.GameDifficulty.Relaxed));
        mediumButton.onClick.AddListener(() => SetGameDifficulty(GameManager.GameDifficulty.Focused));
        hardButton.onClick.AddListener(() => SetGameDifficulty(GameManager.GameDifficulty.Intense));

        UpdateDifficultyButtonVisuals(GameManager.gameDifficulty); // Set initial visuals
    }

    private void SetGameDifficulty(GameManager.GameDifficulty difficulty)
    {
        GameManager.Instance.SetGameDifficulty(difficulty); // Set difficulty in GameManager
        UpdateDifficultyButtonVisuals(difficulty); // Update button visuals
    }

    private void UpdateDifficultyButtonVisuals(GameManager.GameDifficulty selectedDifficulty)
    {
        // Highlight the selected button and dim the others
        easyButton.interactable = selectedDifficulty != GameManager.GameDifficulty.Relaxed;
        mediumButton.interactable = selectedDifficulty != GameManager.GameDifficulty.Focused;
        hardButton.interactable = selectedDifficulty != GameManager.GameDifficulty.Intense;
    }

    private void PopulateLevelSelectButtons()
    {
        foreach (var level in levels)
        {
            GameObject buttonObject = Instantiate(levelSelectButtonPrefab, levelSelectScrollViewContent);
            Button button = buttonObject.GetComponent<Button>();
            TextMeshProUGUI buttonText = buttonObject.GetComponentInChildren<TextMeshProUGUI>();
            Image buttonImage = buttonObject.transform.Find("Level Image").GetComponent<Image>(); // Find the "Level Image" component

            if (buttonText != null)
            {
                buttonText.text = level.levelName; // Set button text to the level name
            }

            if (buttonImage != null)
            {
                buttonImage.sprite = level.levelImage; // Set the button image to the level's image
                buttonImage.enabled = level.levelImage != null; // Enable or disable the image based on its availability
            }

            button.onClick.AddListener(() => SetSelectedLevel(level)); // Assign the level selection action
            UpdateLevelStatsUI(buttonObject, level); // Update the level stats UI
        }
    }

    private void UpdateLevelStatsUI(GameObject buttonObject, GameManager.LevelData level)
    {
        TextMeshProUGUI statsText = buttonObject.transform.Find("Level Stats Text").GetComponent<TextMeshProUGUI>();
        var stats = GameManager.Instance.LoadLevelStats(level.sceneName); // Load level stats from GameManager
        if (stats != null)
        {
            if (stats.isInProgress)
            {
                statsText.text = $"<b>In Progress Time:</b> {FormatTime(stats.inProgressTime)}\n\n<b>Difficulty:</b> {stats.hardestDifficulty}";
            }
            else if (stats.bestTime < float.MaxValue)
            {
                statsText.text = $"<b>Best Time:</b> {FormatTime(stats.bestTime)}\n\n<b>Hardest Difficulty Completed:</b> {stats.hardestDifficulty}";
            }
            else
            {
                statsText.text = ""; // Blank if not played yet
            }
        }
        else
        {
            statsText.text = ""; // Blank if no stats available
        }
    }

    private string FormatTime(float timeInSeconds)
    {
        int hours = Mathf.FloorToInt(timeInSeconds / 3600);
        int minutes = Mathf.FloorToInt((timeInSeconds % 3600) / 60);
        int seconds = Mathf.FloorToInt(timeInSeconds % 60);
        return $"{hours:D2}:{minutes:D2}:{seconds:D2}";
    }

    public void SetActiveCamera(CinemachineVirtualCamera newCamera)
    {
        // Debug.Log("Switching to camera: " + newCamera.name);
        if (activeCamera != null)
        {
            activeCamera.Priority = 0; // Lower priority so it is not active
        }

        activeCamera = newCamera;
        activeCamera.Priority = 10; // Higher priority to make it active
    }

    public void LoadLevel()
    {
        if (selectedLevel != null)
        {
            SetActiveCamera(mainMenuCamera); // Set the loading screen camera as active
            List<Button> buttons = new(FindObjectsOfType<Button>());
            foreach (Button button in buttons)
            {
                button.interactable = false; // Disable all buttons
            }
            // Start loading the level asynchronously
            StartCoroutine(MainMenuLoadLevelAsync(selectedLevel.sceneName));
        }
        else
        {
            Debug.LogError("No level selected to load.");
        }
    }

    public IEnumerator MainMenuLoadLevelAsync(string levelName)
    {
        // Wait for 0.05 seconds to allow the camera to blend
        yield return new WaitForSeconds(0.05f);

        // Wait for the camera to finish blending
        CinemachineBrain brain = mainCamera.GetComponent<CinemachineBrain>();
        if (brain != null)
        {
            while (brain.IsBlending)
            {
                yield return null; // Wait for the next frame while blending is in progress
            }
        }

        SceneLoader.Instance.StartSceneTransitionCoroutine(levelName);

        StartThrobberLoop();
        dominoRain.gameObject.SetActive(true);

        float elapsedTime = 0f; // Track elapsed time
        float fakeProgress = 0f; // Simulated progress value

        // Display the loading screen and update progress
        while (SceneLoader.asyncLoad == null){ yield return null; } // Wait for asyncLoad to be initialized
        while (SceneLoader.asyncLoad != null && !SceneLoader.asyncLoad.isDone)
        {
            // Simulate gradual progress with noise
            if (fakeProgress <= 1f)
            {
                fakeProgress += Time.deltaTime / minimumLoadingTime + UnityEngine.Random.Range(0.002f, 0.05f) * Time.deltaTime;
                fakeProgress = Mathf.Clamp(fakeProgress, 0f, 1f);
            }

            // Use the higher of the fake progress or the actual progress
            float displayedProgress = Mathf.Min(fakeProgress, SceneLoader.asyncLoad.progress / 0.9f);

            // Update loading text
            if (loadingText != null)
            {
                loadingText.text = $"Loading... {Mathf.RoundToInt(displayedProgress * 100)}%";
            }

            // Update circular progress bar
            if (circularProgressBar != null)
            {
                circularProgressBar.fillAmount = displayedProgress; // Set the fill amount based on progress
            }

            // // Allow scene activation after progress reaches 90% and at least the minimum loading time has passed
            // if (SceneLoader.asyncLoad.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
            // {
            //     dominoRain.gameObject.SetActive(true); // Activate the domino rain, which will send back a message to allow the scene activation
            // }

            elapsedTime += Time.deltaTime; // Increment elapsed time
            yield return null; // Wait for the next frame
        }
    }

    private void StartThrobberLoop()
    {
        DominoThrobber[] throbberComponents = throbber.GetComponentsInChildren<DominoThrobber>();
        foreach (var throbberComponent in throbberComponents)
        {
            throbberComponent.BeginLoop(); // Start the throbber loop for each throbber
        }
    }
}
