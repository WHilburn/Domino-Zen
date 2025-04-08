using UnityEngine;
using System.Collections.Generic;

public class DominoResetManager : MonoBehaviour
{
    public static DominoResetManager Instance { get; private set; }
    public HashSet<Domino> fallenDominoes = new();
    private HashSet<Domino> checkpointedDominoes = new(); // Dominoes locked at checkpoints
    private HashSet<Domino> waitingForCheckpoint = new(); // Dominoes that are waiting until the next checkpoint to lock
    public float resetDelay = 2f;
    public float resetDuration = 1f;
    public Domino.DominoAnimation resetAnimation = Domino.DominoAnimation.Rotate;
    public int checkpointThreshold = 5; // Number of dominoes required for a checkpoint

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;

        Domino.OnDominoFall.AddListener(RegisterDominoForReset);
        Domino.OnDominoDeleted.AddListener(RemoveDomino); // Subscribe to domino deletion event
        Domino.OnDominoPlacedCorrectly.AddListener(RegisterDominoPlacement); // Subscribe to domino placement event
        Invoke("UpdateDifficulty", 0.05f); // Update difficulty after a short delay
        // Add all dominos in the scene to checkpointedDominoes
    }

    public void UpdateDifficulty() // Set the game difficulty
    {
        var difficulty = GameManager.Instance.gameDifficulty;
        switch (difficulty)
        {
            case GameManager.GameDifficulty.Easy:
                checkpointThreshold = 1;
                break;
            case GameManager.GameDifficulty.Medium:
                checkpointThreshold = 100;
                break;
            case GameManager.GameDifficulty.Hard:
                checkpointThreshold = 10000;
                break;
        }
        foreach (var domino in FindObjectsOfType<Domino>())
        {
            if (domino.stablePositionSet && difficulty != GameManager.GameDifficulty.Hard) // Only add dominoes with a stable position set
            {
                checkpointedDominoes.Add(domino);
            }
        }
    }

    private void RegisterDominoPlacement(Domino domino) // Registers that a domino was placed in an indicator
    {
        if (GameManager.Instance.gameDifficulty == GameManager.GameDifficulty.Hard) return; // Skip on hard mode
        RemoveDomino(domino); // Remove the domino from the reset list if it was placed correctly
        waitingForCheckpoint.Add(domino); // Add to waitingForCheckpoint set
        if (waitingForCheckpoint.Count % checkpointThreshold == 0)
        {
            LockWaitingDominoes(); // Lock dominoes in waitingForCheckpoint set
            // Debug.Log("Checkpoint reached! Locked dominoes count: " + checkpointedDominoes.Count);
        }
        
    }

    private void RegisterDominoForReset(Domino domino) // Registers that a domino fell and needs to be reset
    {
        if (Instance == null || GameManager.Instance.gameDifficulty == GameManager.GameDifficulty.Hard) return; // Ensure the instance is not null
        if (!fallenDominoes.Contains(domino))
        {
            // Debug.Log("Registering domino as fallen: " + domino.name);
            fallenDominoes.Add(domino);
        }

        CancelInvoke(nameof(ResetAllDominoes));
        Invoke(nameof(ResetAllDominoes), resetDelay);
    }

    private void RemoveDomino(Domino domino)
    {
        if (fallenDominoes.Contains(domino))
        {
            // Debug.Log("Removing domino from fallen list: " + domino.name);
            fallenDominoes.Remove(domino);
        }
    }

    private void LockDomino(Domino domino)
    {
        domino.locked = true;
        checkpointedDominoes.Add(domino);
    }

    private void LockWaitingDominoes()
    {
        foreach (var domino in waitingForCheckpoint)
        {
            LockDomino(domino);
        }
        waitingForCheckpoint.Clear(); // Clear the waitingForCheckpoint set
    }

    private void ResetAllDominoes()
    {
        

        if (GameManager.Instance.gameDifficulty == GameManager.GameDifficulty.Hard)
        {
            Debug.Log("No dominoes will reset on Hard difficulty.");
            return; // No dominoes reset on Hard
        }
        else Debug.Log("Resetting all dominoes. Count: " + fallenDominoes.Count);

        if (fallenDominoes.Count < 1000) resetAnimation = Domino.DominoAnimation.Jump;
        else if (fallenDominoes.Count < 2000) resetAnimation = Domino.DominoAnimation.Rotate;
        else resetAnimation = Domino.DominoAnimation.Teleport;

        foreach (var domino in fallenDominoes)
        {
            if (!domino.stablePositionSet) //Delete dominos that don't have a stable position set
            {
                domino.DespawnDomino();
                continue;
            }
            if (!checkpointedDominoes.Contains(domino)) // Skip resetting dominoes that are not checkpointed
            {
                continue; 
            }
            domino.AnimateDomino(resetAnimation);
        }
        fallenDominoes.Clear();
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.R))
        {
            ResetAllDominoes();
        }
    }
}
