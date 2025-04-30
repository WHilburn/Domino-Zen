using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SceneLoader : MonoBehaviour
{
    public static AsyncOperation asyncLoad = null; // Store the async load operation
    public static SceneLoader Instance { get; private set; } // Singleton instance
    public DominoRain dominoRain; // Reference to the DominoRain script

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this); // Destroy the new instance if one already exists
            return;
        }
        Instance = this; // Set the singleton instance
        DontDestroyOnLoad(this.gameObject);
    }

    public void StartSceneTransitionCoroutine(string sceneName)
    {
        // Begin loading the scene asynchronously
        asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false; // Prevent automatic scene activation
        if (SceneManager.GetActiveScene().name != "Main Menu")
        {
            dominoRain.gameObject.SetActive(true);
        }
    }

    public void CompleteSceneTransition()
    {
        StartCoroutine(CompleteSceneTransitionCoroutine());
    }

    private IEnumerator CompleteSceneTransitionCoroutine()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        // Debug.Log("Unloading scene: " + currentSceneName);
        AudioListener audioListener = FindObjectOfType<AudioListener>();
        if (audioListener != null) audioListener.enabled = false; // Disable the AudioListener
        
        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null) eventSystem.enabled = false; // Disable the EventSystem
        DOTween.KillAll();
        // Wait for the new scene to activate
        if (asyncLoad != null)
        {
            Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1); // Get the last loaded scene
            asyncLoad.allowSceneActivation = true; // Allow scene activation
            while (!asyncLoad.isDone)
            {
                yield return null;
            }
            asyncLoad = null; // Reset the async load operation
        }
        yield return SceneManager.UnloadSceneAsync(currentSceneName);
        StartCoroutine(DisableTransition()); // Start the coroutine to disable the transition
    }

    private IEnumerator DisableTransition()
    {
        yield return new WaitForSeconds(3f);
        dominoRain.gameObject.SetActive(false);
    }
}