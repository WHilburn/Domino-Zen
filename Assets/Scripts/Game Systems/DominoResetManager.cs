using UnityEngine;
using System.Collections.Generic;

public class DominoResetManager : MonoBehaviour
{
    public static DominoResetManager Instance { get; private set; }

    public HashSet<Domino> allDominoes = new(); // All dominoes in the scene
    public HashSet<Domino> fallenDominoes = new();
    public HashSet<Domino> checkpointedDominoes = new(); // Dominoes locked at checkpoints
    private HashSet<Domino> waitingForCheckpoint = new(); // Dominoes that are waiting until the next checkpoint to lock
    public float resetDelay = 1f;
    public float resetDuration = 1f;
    public Domino.DominoAnimation resetAnimation = Domino.DominoAnimation.Rotate;
    public int checkpointThreshold = 5; // Number of dominoes required for a checkpoint
    public enum ResetState {Idle, ResetUpcoming, Resetting};
    public ResetState currentState = ResetState.Idle; // Current state of the reset manager

    private void Awake()
    {
        Domino.OnDominoCreated.AddListener(domino => allDominoes.Add(domino)); // Add domino to allDominoes set
        Domino.OnDominoFall.AddListener(RegisterDominoForReset);
        Domino.OnDominoDeleted.AddListener(RemoveDomino); // Subscribe to domino deletion event
        Domino.OnDominoPlacedCorrectly.AddListener(RegisterDominoPlacement); // Subscribe to domino placement event
        Invoke("UpdateDifficulty", 0.05f); // Update difficulty after a short delay
    }

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;

        // allDominoes = new HashSet<Domino>(FindObjectsOfType<Domino>());
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
        foreach (var domino in allDominoes)
        {
            if (domino.stablePositionSet && difficulty != GameManager.GameDifficulty.Hard) // Only add dominoes with a stable position set
            {
                checkpointedDominoes.Add(domino);
                domino.locked = true; // Lock the domino
            }
        }
    }

    private void RegisterDominoPlacement(Domino domino) // Registers that a domino was placed in an indicator
    {
        if (GameManager.Instance.gameDifficulty == GameManager.GameDifficulty.Hard) return; // Skip on hard mode
        if (fallenDominoes.Contains(domino))
        {
            fallenDominoes.Remove(domino); // Remove the domino from the reset list if it was placed correctly
        } 
        waitingForCheckpoint.Add(domino); // Add to waitingForCheckpoint set
        if (waitingForCheckpoint.Count % checkpointThreshold == 0)
        {
            CheckpointWaitingDominoes(); // Lock dominoes in waitingForCheckpoint set
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

        if (!domino.CheckUpright()){ //Only start a domino reset if the domino is not upright
            CancelInvoke(nameof(ResetAllDominoes));
            Invoke(nameof(ResetAllDominoes), resetDelay);
            currentState = ResetState.ResetUpcoming; // Set the state to ResetUpcoming
        }
    }

    private void RemoveDomino(Domino domino)
    {
        if (fallenDominoes.Contains(domino))
        {
            fallenDominoes.Remove(domino);
        }
        if (allDominoes.Contains(domino))
        {
            allDominoes.Remove(domino);
        }
    }

    private void CheckpointDomino(Domino domino)
    {
        domino.locked = true;
        checkpointedDominoes.Add(domino);

        if (domino.placementIndicator != null)
        {
            domino.placementIndicator.gameObject.SetActive(false); // Disable the placement indicator
            domino.placementIndicator = null; // Clear the reference to the placementIndicator
        }
    }

    private void CheckpointWaitingDominoes()
    {
        foreach (var domino in waitingForCheckpoint)
        {
            CheckpointDomino(domino);
        }
        waitingForCheckpoint.Clear(); // Clear the waitingForCheckpoint set
    }

    private void ResetAllDominoes()
    {
        // if (fallenDominoes.Count == 0) return; // No dominoes to reset
        if (currentState == ResetState.Resetting || GameManager.Instance.gameDifficulty == GameManager.GameDifficulty.Hard)
        {
            return;
        }
        else Debug.Log("Resetting all dominoes. Count: " + allDominoes.Count);

        if (allDominoes.Count < 2000)
        {
            resetAnimation = Domino.DominoAnimation.Jump;
            currentState = ResetState.Resetting;
        } 
        else
        {
            currentState = ResetState.Idle;
            resetAnimation = Domino.DominoAnimation.Teleport;
        } 
        float resetDuration = 1f;
        Invoke(nameof(ResetToIdle), resetDuration * 1.5f); // Reset the state to Idle after the reset duration
        
        PlayerDominoPlacement.Instance.DeleteHeldDomino();

        foreach (var domino in allDominoes)
        {
            if (checkpointedDominoes.Contains(domino)) // Reset checkpointed dominoes
            {
                domino.AnimateDomino(resetAnimation, resetDuration);
            }
            else //Delete all non-checkpointed dominoes
            {
                domino.DespawnDomino();
                continue;
            }
        }
        fallenDominoes.Clear();
        waitingForCheckpoint.Clear(); // Clear the waitingForCheckpoint set
        CancelInvoke(nameof(ResetAllDominoes));
    }

    private void ResetToIdle()
    {
        currentState = ResetState.Idle; // Set the state to Idle
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ResetAllDominoes();
        }
    }
}
