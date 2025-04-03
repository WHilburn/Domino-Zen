using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using DG.Tweening;

public class MainMenuManager : MonoBehaviour
{
    public TextMeshProUGUI loadingText;

    public GameObject throbber;
    public float minimumLoadingTime = 2f; // Minimum loading time in seconds

    // Reference to the main camera and virtual cameras
    public Camera mainCamera;
    public CinemachineVirtualCamera mainMenuCamera;
    public CinemachineVirtualCamera optionsMenuCamera;
    public CinemachineVirtualCamera levelSelectCamera;
    public CinemachineVirtualCamera loadingScreenCamera;

    public Image circularProgressBar; // Reference to the circular progress bar image
    
    // Store current active camera
    private CinemachineVirtualCamera activeCamera;
    public AsyncOperation asyncLoad = null; // Store the async load operation
    public DominoRain dominoRain; // Reference to the DominoRain script for scene transitions
    
    private void Start()
    {
        // Set the main menu camera as the default
        SetActiveCamera(mainMenuCamera);
        DOTween.defaultRecyclable = true;
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
        if (activeCamera == loadingScreenCamera){
            throbber.SetActive(true);
            LoadLevel("Progress Scene");
        } 
    }

    // Called when a level button is clicked
    public void LoadLevel(string levelName)
    {
        Debug.Log("Loading level: " + levelName);
        // Start loading the level asynchronously
        StartCoroutine(LoadLevelAsync(levelName));
    }

    public IEnumerator LoadLevelAsync(string levelName)
    {
        // Wait for 0.1 seconds to allow the camera to blend
        yield return new WaitForSeconds(0.1f);

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

        DominoThrobber[] throbberComponents = throbber.GetComponentsInChildren<DominoThrobber>();
        foreach (var throbberComponent in throbberComponents)
        {
            throbberComponent.BeginLoop(); // Start the throbber loop for each throbber
        }

        float elapsedTime = 0f; // Track elapsed time
        float fakeProgress = 0f; // Simulated progress value

        // Display the loading screen and update progress
        while (!asyncLoad.isDone)
        {
            // Simulate gradual progress with noise
            if (fakeProgress <= 1f)
            {
                fakeProgress += Time.deltaTime / minimumLoadingTime + Random.Range(0.002f, 0.05f) * Time.deltaTime;
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
