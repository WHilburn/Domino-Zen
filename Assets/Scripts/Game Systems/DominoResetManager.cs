using UnityEngine;
using System.Collections.Generic;
using System.Collections;
using UnityEngine.Events;

public class DominoResetManager : MonoBehaviour
{
    #region Singleton
    public static DominoResetManager Instance { get; private set; }
    #endregion

    #region Fields
    public HashSet<Domino> allDominoes = new(); // All dominoes in the scene
    public HashSet<Domino> fallenDominoes = new();
    public HashSet<Domino> checkpointedDominoes = new(); // Dominoes locked at checkpoints
    private HashSet<Domino> waitingForCheckpoint = new(); // Dominoes that are waiting until the next checkpoint to lock
    public static float resetDelay = 1.2f;
    public static float timeUntilReset;
    public static float resetDuration = 1f;
    public Domino.DominoAnimation resetAnimation = Domino.DominoAnimation.Rotate;
    public int checkpointThreshold = 5; // Number of dominoes required for a checkpoint
    public enum ResetState {Idle, ResetUpcoming, Resetting};
    public ResetState currentState = ResetState.Idle; // Current state of the reset manager
    public static UnityEvent OnResetStart = new();
    public static UnityEvent OnResetEnd = new();
    public static UnityEvent OnResetUpcoming = new();
    #endregion

    #region Unity Lifecycle
    private void Awake()
    {
        Domino.OnDominoCreated.AddListener(domino => allDominoes.Add(domino)); // Add domino to allDominoes set
        Domino.OnDominoFall.AddListener(RegisterDominoForReset);
        Domino.OnDominoDeleted.AddListener(RemoveDomino); // Subscribe to domino deletion event
        Domino.OnDominoPlacedCorrectly.AddListener(RegisterDominoPlacement); // Subscribe to domino placement event
        GameManager.OnGameDifficultyChanged.AddListener(UpdateDifficulty); // Subscribe to difficulty change event
        Invoke(nameof(InvokeUpdateDifficulty), 0.05f);
        timeUntilReset = resetDelay; // Initialize timeUntilReset to resetDelay
    }

    private void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
    }

    private void InvokeUpdateDifficulty()
    {
        UpdateDifficulty(GameManager.Instance.gameDifficulty);
    }

    private void Update()
    {
        if (Input.GetKeyDown(KeyCode.M))
        {
            ResetAllDominoes();
        }
        timeUntilReset -= Time.deltaTime; // Decrease the time until reset
        timeUntilReset = Mathf.Clamp(timeUntilReset, 0, resetDelay); // Clamp the time until reset to a minimum of 0
    }

    private void OnDestroy()
    {
        GameManager.OnGameDifficultyChanged.RemoveListener(UpdateDifficulty); // Unsubscribe to avoid memory leaks
    }
    #endregion

    #region Difficulty Management
    public void UpdateDifficulty(GameManager.GameDifficulty difficulty) // Set the game difficulty
    {
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
                checkpointedDominoes.Clear(); // Clear checkpointed dominoes on hard mode
                foreach (var domino in allDominoes)
                {
                    domino.locked = false; // Unlock all dominoes on hard mode
                    domino.stablePositionSet = false; // Reset stable position set
                    domino.placementIndicator = null; // Clear the placement indicator
                }
                break;
        }
        foreach (var domino in allDominoes)
        {
            if (domino.stablePositionSet && difficulty != GameManager.GameDifficulty.Hard) // Only add dominoes with a stable position set
            {
                checkpointedDominoes.Add(domino);
                if (domino.gameObject.name != "Initiator") domino.locked = true; // Lock the domino unless it's for initiating
            }
        }
    }
    #endregion

    #region Domino Registration
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
        else return; // A domino cannot trigger a reset more than once

        if (!domino.CheckUpright() || domino.stablePositionSet)
        {
            CancelInvoke(nameof(ResetAllDominoes));
            Invoke(nameof(ResetAllDominoes), resetDelay);
            timeUntilReset = resetDelay; // Reset the time until reset
            currentState = ResetState.ResetUpcoming; // Set the state to ResetUpcoming
            OnResetUpcoming.Invoke(); // Trigger OnResetUpcoming event
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
    #endregion

    #region Checkpoint Management
    private void CheckpointDomino(Domino domino)
    {
        domino.locked = true;
        checkpointedDominoes.Add(domino);

        if (domino.placementIndicator != null)
        {
            StartCoroutine(DeactivateIndicator(domino, domino.placementIndicator)); // Deactivate the placement indicator after a delay
        }
    }

    private IEnumerator DeactivateIndicator(Domino domino, PlacementIndicator indicator)
    {
        yield return new WaitForSeconds(PlacementIndicator.fadeSpeed); // Wait for a short duration
        indicator.gameObject.SetActive(false); // Disable the placement indicator
        domino.placementIndicator = null; // Clear the reference to the placementIndicator
    }

    private void CheckpointWaitingDominoes()
    {
        foreach (var domino in waitingForCheckpoint)
        {
            CheckpointDomino(domino);
        }
        waitingForCheckpoint.Clear(); // Clear the waitingForCheckpoint set
    }
    #endregion

    #region Reset Management
    public void ResetAllDominoes()
    {
        // if (fallenDominoes.Count == 0) return; // No dominoes to reset
        if (currentState == ResetState.Resetting || GameManager.Instance.gameDifficulty == GameManager.GameDifficulty.Hard)
        {
            return;
        }
        else Debug.Log("Resetting all dominoes. Count: " + allDominoes.Count);

        OnResetStart.Invoke(); // Trigger OnResetStart event

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
        Invoke(nameof(ResetToIdle), resetDuration * 1.25f); // Reset the state to Idle after the reset duration
        
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
        OnResetEnd.Invoke(); // Trigger OnResetEnd event
    }
    #endregion
}
