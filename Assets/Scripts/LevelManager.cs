using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class LevelManager : MonoBehaviour
{
    public List<PlacementIndicator> allIndicators;  // List of all dominoes in the scene
    private bool physicsEnabled = true; // Whether domino physics are enabled

    void Start()
    {
        foreach (var indicator in FindObjectsOfType<PlacementIndicator>())
        {
            allIndicators.Add(indicator);
        }
    }

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            UnityEngine.SceneManagement.SceneManager.LoadScene(UnityEngine.SceneManagement.SceneManager.GetActiveScene().name);
        }   
        if (Input.GetKeyDown(KeyCode.P))
        {
            ToggleDominoPhysics();
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

    private void ToggleDominoPhysics()
    {
        physicsEnabled = !physicsEnabled; // Flip state
        Debug.Log($"Domino physics is now {(physicsEnabled ? "ENABLED" : "DISABLED")}");
        
        // Find all dominoes (assumes they have a "Domino" tag or a common component)
        GameObject[] dominoes = GameObject.FindGameObjectsWithTag("DominoTag");

        foreach (GameObject domino in dominoes)
        {
            Rigidbody rb = domino.GetComponent<Rigidbody>();
            MonoBehaviour[] scripts = domino.GetComponents<MonoBehaviour>();
            BoxCollider collider = domino.GetComponent<BoxCollider>();

            if (rb != null)
            {
                rb.isKinematic = !physicsEnabled; // Toggle physics
                rb.useGravity = physicsEnabled; // Ensure gravity behaves correctly
                collider.enabled = physicsEnabled;
            }

            foreach (MonoBehaviour script in scripts)
            {
                script.enabled = physicsEnabled; // Toggle all scripts
            }
        }
    }
}
