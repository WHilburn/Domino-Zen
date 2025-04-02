using System.Collections;
using Cinemachine;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

public class MainMenuManager : MonoBehaviour
{
    public GameObject levelSelectPanel;
    public GameObject optionsPanel;
    public GameObject loadingScreenPanel;
    public TextMeshProUGUI loadingText;

    // Reference to the main camera and virtual cameras
    public Camera mainCamera;
    public CinemachineVirtualCamera mainMenuCamera;
    public CinemachineVirtualCamera optionsMenuCamera;
    public CinemachineVirtualCamera levelSelectCamera;
    public CinemachineVirtualCamera loadingScreenCamera;
    
    // Store current active camera
    private CinemachineVirtualCamera activeCamera;
    
    private void Start()
    {
        // Set the main menu camera as the default
        SetActiveCamera(mainMenuCamera);
    }

    public void SetActiveCamera(CinemachineVirtualCamera newCamera)
    {
        Debug.Log("Switching to camera: " + newCamera.name);
        if (activeCamera != null)
        {
            activeCamera.Priority = 0; // Lower priority so it is not active
        }

        activeCamera = newCamera;
        activeCamera.Priority = 10; // Higher priority to make it active
    }

    // Called when a level button is clicked
    public void LoadLevel(string levelName)
    {
        Debug.Log("Loading level: " + levelName);
        // Show the loading screen
        // loadingScreenPanel.SetActive(true);
        loadingText.enabled = true; // Enable the loading text
        loadingText.text = "Loading...";

        // Start loading the level asynchronously
        // StartCoroutine(LoadLevelAsync(levelName));
    }

    private IEnumerator LoadLevelAsync(string levelName)
    {
        // Begin loading the scene asynchronously
        AsyncOperation asyncLoad = SceneManager.LoadSceneAsync(levelName);
        asyncLoad.allowSceneActivation = false; // Prevent automatic scene activation

        while (!asyncLoad.isDone)
        {
            // Optionally, update progress bar or display a loading message here
            if (asyncLoad.progress >= 0.9f)
            {
                // Fade out loading screen (if using canvas group)
                // FadeIn logic or a slight delay before activating scene
                asyncLoad.allowSceneActivation = true;
            }

            yield return null;
        }
    }
}
