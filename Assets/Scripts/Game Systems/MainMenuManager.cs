using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections.Generic;
using System;

[Serializable]
public class LevelData
{
    public string levelName; // Name of the level
    public string sceneName; // Unity scene associated with the level
    [TextArea] public string description; // Description text for the level
    public Sprite levelImage; // Image associated with the level
}

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
    public AsyncOperation asyncLoad = null; // Store the async load operation
    public DominoRain dominoRain; // Reference to the DominoRain script for scene transitions
    public GameObject levelSelectButtonPrefab; // Prefab for level select buttons
    public Transform levelSelectScrollViewContent; // Content transform of the scroll view

    public Button easyButton;
    public Button mediumButton;
    public Button hardButton;

    public List<LevelData> levels = new List<LevelData>(); // List of levels, editable in the Unity Editor

    private LevelData selectedLevel; // Currently selected level

    private void Start()
    {
        // Set the main menu camera as the default
        SetActiveCamera(mainMenuCamera);
        DOTween.defaultRecyclable = true;

        PopulateLevelSelectButtons(); // Create level select buttons
        InitializeDifficultyButtons();
        SetSelectedLevel(levels.Count > 0 ? levels[0] : null); // Set the default selected level
        UpdateLevelPreviewImage(); // Update the preview image for the default selected level
    }

    public void SetSelectedLevel(LevelData level)
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
            SetGameDifficulty(GameManager.GameDifficulty.Easy);
            easyButton.gameObject.SetActive(false); // Disable the easy button to prevent changing difficulty
            mediumButton.gameObject.SetActive(false);
            hardButton.gameObject.SetActive(false);
        }
        else
        {
            easyButton.gameObject.SetActive(true);
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
        easyButton.onClick.AddListener(() => SetGameDifficulty(GameManager.GameDifficulty.Easy));
        mediumButton.onClick.AddListener(() => SetGameDifficulty(GameManager.GameDifficulty.Medium));
        hardButton.onClick.AddListener(() => SetGameDifficulty(GameManager.GameDifficulty.Hard));

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
        easyButton.interactable = selectedDifficulty != GameManager.GameDifficulty.Easy;
        mediumButton.interactable = selectedDifficulty != GameManager.GameDifficulty.Medium;
        hardButton.interactable = selectedDifficulty != GameManager.GameDifficulty.Hard;
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
        }
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
            StartCoroutine(LoadLevelAsync(selectedLevel.sceneName));
        }
        else
        {
            Debug.LogError("No level selected to load.");
        }
    }

    public IEnumerator LoadLevelAsync(string levelName)
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

        // Begin loading the scene asynchronously
        asyncLoad = SceneManager.LoadSceneAsync(levelName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false; // Prevent automatic scene activation

        StartThrobberLoop();

        float elapsedTime = 0f; // Track elapsed time
        float fakeProgress = 0f; // Simulated progress value

        // Display the loading screen and update progress
        while (!asyncLoad.isDone)
        {
            // Simulate gradual progress with noise
            if (fakeProgress <= 1f)
            {
                fakeProgress += Time.deltaTime / minimumLoadingTime + UnityEngine.Random.Range(0.002f, 0.05f) * Time.deltaTime;
                fakeProgress = Mathf.Clamp(fakeProgress, 0f, 1f);
            }

            // Use the higher of the fake progress or the actual progress
            float displayedProgress = Mathf.Min(fakeProgress, asyncLoad.progress / 0.9f);

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

            // Allow scene activation after progress reaches 90% and at least the minimum loading time has passed
            if (asyncLoad.progress >= 0.9f && elapsedTime >= minimumLoadingTime)
            {
                // asyncLoad.allowSceneActivation = true;
                dominoRain.gameObject.SetActive(true); // Activate the domino rain, which will send back a message to allow the scene activation
            }

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

    public void CompleteSceneTransitions()
    {
        StartCoroutine(CompleteSceneTransitionCoroutine());
    }

    private IEnumerator CompleteSceneTransitionCoroutine()
    {
        // Kill all tween animations
        DOTween.KillAll();

        // Wait for the new scene to activate
        if (asyncLoad != null)
        {
            mainCamera.GetComponent<AudioListener>().enabled = false;
            asyncLoad.allowSceneActivation = true; // Allow scene activation
            while (!asyncLoad.isDone)
            {
                yield return null; // Wait until the scene is fully loaded
            }
            asyncLoad = null; // Reset the async load operation
        }

        // Unload the previous scene
        string currentSceneName = SceneManager.GetActiveScene().name;
        if (currentSceneName == "Main Menu")
        {
            yield return SceneManager.UnloadSceneAsync("Main Menu");
        }

        // Disable the main camera's audio listener
        
    }
}
