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
    private bool isTracking = false;
    public List<Transform> fallingDominoes = new();
    public static UnityEvent OnFreeLookCameraEnabled = new();
    public static UnityEvent OnFreeLookCameraDisabled = new();

    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);

        Domino.OnDominoFall.AddListener(HandleDominoFall);
        Domino.OnDominoStopMoving.AddListener(HandleDominoStopMoving);
        Domino.OnDominoDeleted.AddListener(HandleDominoDeleted);

        EnableFreeLook(); // Start with player control
    }

    void OnDestroy()
    {
        Domino.OnDominoFall.RemoveListener(HandleDominoFall);
        Domino.OnDominoStopMoving.RemoveListener(HandleDominoStopMoving);
        Domino.OnDominoDeleted.RemoveListener(HandleDominoDeleted);
    }

    private void HandleDominoFall(Domino domino)
    {
        if (!fallingDominoes.Contains(domino.transform))
        {
            fallingDominoes.Add(domino.transform);
        }
    }

    private void HandleDominoStopMoving(Domino domino)
    {
        fallingDominoes.Remove(domino.transform);
    }

    private void HandleDominoDeleted(Domino domino)
    {
        fallingDominoes.Remove(domino.transform);
    }

    void Update()
    {
        if (fallingDominoes.Count >= 2)
        {
            TrackDominoes();
        }
        else if (fallingDominoes.Count == 0)
        {
            StopTracking();
        }
    }

    public void TrackDominoes()
    {
        if (!isTracking) EnableTrackingCamera(); // Switch to tracking camera if not already tracking
        isTracking = true;

        // Add dominoes to target group with smooth weight adjustments
        targetGroup.m_Targets = new CinemachineTargetGroup.Target[fallingDominoes.Count];
        for (int i = 0; i < fallingDominoes.Count; i++)
        {
            targetGroup.m_Targets[i] = new CinemachineTargetGroup.Target
            {
                target = fallingDominoes[i],
                weight = 1,
                radius = 3f
            };
        }
    }

    public void StopTracking()
    {
        if (!isTracking) return;
        isTracking = false;
        EnableFreeLook();
    }
    private void EnableFreeLook()
    {
        // Debug.Log("Enabling FreeLook Camera");
        freeLookCamera.Priority = 20;
        trackingCamera.Priority = 10;
        freeLookCamera.GetComponent<PlayerCameraController>().InitializeRotation();
        OnFreeLookCameraEnabled.Invoke(); // Invoke the event when free look camera is enabled
    }

    private void EnableTrackingCamera()
    {
        // Debug.Log("Enabling Tracking Camera");
        freeLookCamera.Priority = 10;
        trackingCamera.Priority = 20;
        GetComponent<PlayerDominoPlacement>().ReleaseDomino();
        OnFreeLookCameraDisabled.Invoke();
    }
}
