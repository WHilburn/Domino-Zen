using System.Collections;
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
    
    // Called when "Select Level" is clicked
    public void OpenLevelSelect()
    {
        levelSelectPanel.SetActive(true);
    }

    // Called when "Options" is clicked
    public void OpenOptions()
    {
        optionsPanel.SetActive(true);
    }

    // Called when the back button on the Options panel is clicked
    public void CloseOptions()
    {
        optionsPanel.SetActive(false);
    }

    // Called when a level button is clicked
    public void LoadLevel(string levelName)
    {
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
