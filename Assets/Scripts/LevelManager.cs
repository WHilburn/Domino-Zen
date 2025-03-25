using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public List<PlacementIndicator> allIndicators;  // List of all dominoes in the scene
    // Start is called before the first frame update
    void Start()
    {
        foreach (var indicator in FindObjectsOfType<PlacementIndicator>())
        {
            allIndicators.Add(indicator);
        }
    }

    public void CheckCompletion()
    {
        // check if every indicator is satisfied
        bool allFadedOut = true;
        foreach (var indicator in allIndicators)
        {
            if (!indicator.isFadingOut)
            {
                allFadedOut = false;
                break;
            }
        }
        if (allFadedOut)
        {
            Debug.Log("All indicators faded out!");
        }
    }

    // Update is called once per frame
    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }   
    }
}
