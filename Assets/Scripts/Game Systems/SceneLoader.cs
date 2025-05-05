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
    private bool reloadScene = false; // Flag to indicate if the scene should be reloaded

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(this.gameObject); // Destroy the new instance if one already exists
            return;
        }
        Instance = this; // Set the singleton instance
        DontDestroyOnLoad(this.gameObject);
    }

    void Update()
    {
        transform.position = GameManager.Instance.mainCamera.transform.position; // Keep the loader at the camera position
    }

    public void StartSceneTransitionCoroutine(string sceneName, bool reloadScene = false)
    {
        this.reloadScene = reloadScene; // Set the reload scene flag
        if (!reloadScene)
        {
            if (SceneManager.GetActiveScene().name != "Main Menu")
            {
                LevelProgressManager.SaveProgress(SceneManager.GetActiveScene().name, GameManager.Instance.GetFilledIndicators()); // Save progress when the level is destroyed
            }
            // Begin loading the scene asynchronously
            asyncLoad = SceneManager.LoadSceneAsync(sceneName, LoadSceneMode.Additive);
            asyncLoad.allowSceneActivation = false; // Prevent automatic scene activation
        }
        if (InGameUI.Instance != null)
        {
            InGameUI.Instance.SaveSettings();
        }
        dominoRain.StartRain();
    }

    public void CompleteSceneTransition()
    {
        StartCoroutine(CompleteSceneTransitionCoroutine());
    }

    private IEnumerator CompleteSceneTransitionCoroutine()
    {
        if (!reloadScene)
        {
            string currentSceneName = SceneManager.GetActiveScene().name;
            EventSystem eventSystem = FindObjectOfType<EventSystem>();
            if (eventSystem != null) eventSystem.enabled = false;
            DOTween.KillAll();
            // Wait for the new scene to activate
            if (asyncLoad != null)
            {

                asyncLoad.allowSceneActivation = true; // Allow scene activation
                while (!asyncLoad.isDone)
                {
                    yield return null;
                }
                asyncLoad = null; // Reset the async load operation
            }
            yield return SceneManager.UnloadSceneAsync(currentSceneName);
        }
        else
        {
            SceneManager.LoadScene(SceneManager.GetActiveScene().name);
        }

        // DOTween.KillAll();
        GameManager.gamePaused = false; // Reset the game paused state
        GameManager.levelComplete = false; // Reset the level complete flag
        GameManager.elapsedTime = 0f; // Reset the elapsed time
        GameManager.filledIndicators = 0;
        reloadScene = false;
    }
}