using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.EventSystems;
using DG.Tweening;

public class SceneLoader : MonoBehaviour
{
    public static AsyncOperation asyncLoad = null; // Store the async load operation
    public static SceneLoader Instance { get; private set; } // Singleton instance

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this; // Set the singleton instance
        DontDestroyOnLoad(this.gameObject);
    }

    public void StartSceneTransitionCoroutine(string sceneName)
    {
        // Begin loading the scene asynchronously
        asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
        asyncLoad.allowSceneActivation = false; // Prevent automatic scene activation
    }

    public void CompleteSceneTransition()
    {
        // Start the scene transition coroutine
        StartCoroutine(CompleteSceneTransitionCoroutine());
    }

    private IEnumerator CompleteSceneTransitionCoroutine()
    {
        string currentSceneName = SceneManager.GetActiveScene().name;
        Debug.Log($"Old scene name: {currentSceneName}");
        Debug.Log("Completing scene transition...");

        AudioListener audioListener = FindObjectOfType<AudioListener>();
        if (audioListener != null)
        {
            audioListener.enabled = false; // Disable the AudioListener
        }

        EventSystem eventSystem = FindObjectOfType<EventSystem>();
        if (eventSystem != null)
        {
            eventSystem.enabled = false; // Disable the EventSystem
        }

        Debug.Log("AudioListener and EventSystem disabled.");

        // Kill all tween animations
        DOTween.KillAll();

        // Wait for the new scene to activate
        if (asyncLoad != null)
        {
            Debug.Log("1");
            Scene newScene = SceneManager.GetSceneAt(SceneManager.sceneCount - 1); // Get the last loaded scene
            Debug.Log("2");
            asyncLoad.allowSceneActivation = true; // Allow scene activation
            Debug.Log("3");
            while (!asyncLoad.isDone)
            {
                Debug.Log("4");
                yield return null;
                Debug.Log("5");
            }
            Debug.Log($"New scene activated: {newScene.name}");
            asyncLoad = null; // Reset the async load operation
        }

        Debug.Log($"{currentSceneName} scene unloading...");
        yield return SceneManager.UnloadSceneAsync(currentSceneName);
        asyncLoad = null; // Reset the async load operation
    }
}