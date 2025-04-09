using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;
using UnityEngine.Events;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    public CinemachineVirtualCamera freeLookCamera; // Player-controlled camera
    public CinemachineVirtualCamera trackingCamera; // Auto-framing camera
    public CinemachineTargetGroup targetGroup; // Group of falling dominoes
    public static UnityEvent OnFreeLookCameraEnabled = new();
    public static UnityEvent OnFreeLookCameraDisabled = new();

    private bool isTracking = false;
    private Dictionary<Transform, float> dominoTimers = new(); // Tracks time remaining for each domino in the target group
    private const float dominoLifetime = .5f; // Time before domino is removed from the target group

    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Domino.OnDominoFall.AddListener(HandleDominoFall);
        Domino.OnDominoDeleted.AddListener(HandleDominoDeleted);

        EnableFreeLook(); // Start with player control
    }

    void OnDestroy()
    {
        Domino.OnDominoFall.RemoveListener(HandleDominoFall);
        Domino.OnDominoDeleted.RemoveListener(HandleDominoDeleted);
    }

    private void HandleDominoFall(Domino domino)
    {
        if (!dominoTimers.ContainsKey(domino.transform))
        {
            dominoTimers[domino.transform] = dominoLifetime;
            targetGroup.AddMember(domino.transform, 1f, 0.1f); // Add domino to the target group
        }
    }

    private void HandleDominoDeleted(Domino domino)
    {
        RemoveDominoFromTargetGroup(domino.transform);
    }

    void Update()
    {
        UpdateDominoTimers();

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

    private void UpdateDominoTimers()
    {
        var dominoesToRemove = new List<Transform>();

        // Iterate over a copy of the keys to avoid modifying the collection during enumeration
        foreach (var domino in new List<Transform>(dominoTimers.Keys))
        {
            dominoTimers[domino] -= Time.deltaTime;
            if (dominoTimers[domino] <= 0f)
            {
                dominoesToRemove.Add(domino);
            }
        }

        foreach (var domino in dominoesToRemove)
        {
            RemoveDominoFromTargetGroup(domino);
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

    private void EnableFreeLook()
    {
        isTracking = false;
        freeLookCamera.Priority = 20;
        trackingCamera.Priority = 10;
        freeLookCamera.GetComponent<PlayerCameraController>().InitializeRotation();
        OnFreeLookCameraEnabled.Invoke();
    }

    private void EnableTrackingCamera()
    {
        isTracking = true;
        freeLookCamera.Priority = 10;
        trackingCamera.Priority = 20;
        GetComponent<PlayerDominoPlacement>().ReleaseDomino();
        OnFreeLookCameraDisabled.Invoke();
    }
}
