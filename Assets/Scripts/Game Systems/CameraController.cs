using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.Events;

public class CameraController : MonoBehaviour
{
    #region Fields and Properties
    public static CameraController Instance { get; private set; }
    public CinemachineVirtualCamera freeLookCamera; // Player-controlled camera
    public CinemachineVirtualCamera trackingCamera; // Auto-framing camera
    public CinemachineTargetGroup targetGroup; // Group of falling dominoes
    public static UnityEvent OnFreeLookCameraEnabled = new();
    public static UnityEvent OnFreeLookCameraDisabled = new();

    private bool isTracking = false;
    private Dictionary<Transform, float> dominoTimers = new(); // Tracks time remaining for each domino in the target group
    private const float dominoLifetime = .25f; // Time before domino is removed from the target group
    private HashSet<Transform> trackedDominoes = new(); // Tracks dominoes already added to the target group
    #endregion

    #region Unity Lifecycle
    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;

        // Ensure the tracking camera has a track assigned
        var trackedDolly = trackingCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly != null && trackedDolly.m_Path == null)
        {
            trackedDolly.m_Path = FindObjectOfType<CinemachineSmoothPath>();
            if (trackedDolly.m_Path == null)
            {
                Debug.LogWarning("No track assigned to the tracking camera's Tracked Dolly component, and no track was found in the scene.");
            }
        }

        Domino.OnDominoFall.AddListener(HandleDominoFall);
        Domino.OnDominoDeleted.AddListener(HandleDominoDeleted);

        EnableFreeLook(); // Start with player control
    }

    void OnDestroy()
    {
        Domino.OnDominoFall.RemoveListener(HandleDominoFall);
        Domino.OnDominoDeleted.RemoveListener(HandleDominoDeleted);
    }

    void Update()
    {
        UpdateDominoTimers();
        DrawDebugLines(); // Draw debug lines to the target group

        if (DominoResetManager.Instance.currentState == DominoResetManager.ResetState.Resetting)
        {
            EnableFreeLook();
            return;
        }

        if (dominoTimers.Count >= 2 && !isTracking)
        {
            EnableTrackingCamera();
        }
        else if (dominoTimers.Count == 0 && isTracking)
        {
            EnableFreeLook();
        }
    }
    #endregion

    #region Event Handlers
    private void HandleDominoFall(Domino domino)
    {
        if (!dominoTimers.ContainsKey(domino.transform) && !trackedDominoes.Contains(domino.transform))
        {
            dominoTimers[domino.transform] = dominoLifetime;
            targetGroup.AddMember(domino.transform, 1f, 0.1f); // Add domino to the target group
            trackedDominoes.Add(domino.transform); // Mark domino as tracked
        }
    }

    private void HandleDominoDeleted(Domino domino)
    {
        RemoveDominoFromTargetGroup(domino.transform);
    }
    #endregion

    #region Camera Management
    private void EnableFreeLook()
    {
        isTracking = false;
        freeLookCamera.Priority = 20;
        trackingCamera.Priority = 10;
        freeLookCamera.GetComponent<PlayerCameraController>().InitializeRotation();
        OnFreeLookCameraEnabled.Invoke();
        trackedDominoes.Clear(); // Allow dominoes to be tracked again
    }

    private void EnableTrackingCamera()
    {
        isTracking = true;
        freeLookCamera.Priority = 10;
        trackingCamera.Priority = 20;
        GetComponent<PlayerDominoPlacement>().ReleaseDomino();
        OnFreeLookCameraDisabled.Invoke();
    }
    #endregion

    #region Domino Management
    private void UpdateDominoTimers()
    {
        foreach (var domino in new List<Transform>(dominoTimers.Keys))
        {
            dominoTimers[domino] -= Time.deltaTime;

            // Calculate weight based on remaining time
            float elapsedTime = dominoLifetime - dominoTimers[domino];
            float weight = Mathf.Clamp01(elapsedTime / (dominoLifetime / 2)); // Ramp up
            if (dominoTimers[domino] < dominoLifetime / 2)
            {
                weight = Mathf.Clamp01(dominoTimers[domino] / (dominoLifetime / 2)); // Fade out
            }

            targetGroup.m_Targets[targetGroup.FindMember(domino)].weight = weight;

            if (dominoTimers[domino] <= 0f && domino.GetComponent<Domino>().currentState != Domino.DominoState.Moving)
            {
                RemoveDominoFromTargetGroup(domino);
            }
        }
    }

    private void RemoveDominoFromTargetGroup(Transform domino)
    {
        if (dominoTimers.ContainsKey(domino))
        {
            dominoTimers.Remove(domino);
            targetGroup.RemoveMember(domino); // Remove domino from the target group
        }
    }
    #endregion

    #region Debugging
    private void DrawDebugLines()
    {
        foreach (var domino in dominoTimers.Keys)
        {
            if (domino != null)
            {
                Debug.DrawLine(domino.position, targetGroup.transform.position, Color.blue);
            }
        }
    }
    #endregion
}
