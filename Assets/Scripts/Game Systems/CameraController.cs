using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public static CameraController Instance { get; private set; }
    public CinemachineVirtualCamera freeLookCamera; // Player-controlled camera
    public CinemachineVirtualCamera trackingCamera; // Auto-framing camera
    public CinemachineTargetGroup targetGroup; // Group of falling dominoes

    private bool isTracking = false;
    public List<Transform> fallingDominoes = new List<Transform>();

    public float heightOffset = 2f; // Extra height to prevent low shots
    public float cameraHeightBoost = 10f; // Extra height for tracking camera
    public float smoothTime = 0.5f; // Smoothing time for camera adjustments
    public bool prioritySystem = true; // Use Cinemachine priority system

    void Start()
    {
        if (Instance == null)
            Instance = this;
        else
            Destroy(gameObject);
        EnableFreeLook(); // Start with player control
    }

    void Update()
    {
        if (fallingDominoes.Count >= 2)
        {
            TrackDominoes();
        }
        else
        {
            StopTracking();
        }
    }

    public void TrackDominoes()
    {
        isTracking = true;

        // Add dominoes to target group with smooth weight adjustments
        targetGroup.m_Targets = new CinemachineTargetGroup.Target[fallingDominoes.Count];
        for (int i = 0; i < fallingDominoes.Count; i++)
        {
            // float dominoVelocity = fallingDominoes[i].GetComponent<Rigidbody>().angularVelocity.magnitude;
            // if (dominoVelocity < .1f)
            // {
            //     dominoVelocity = .1f;
            // }
            targetGroup.m_Targets[i] = new CinemachineTargetGroup.Target
            {
                target = fallingDominoes[i],
                weight = 1,
                radius = 3f
            };
        }

        EnableTrackingCamera();
    }

    public void StopTracking()
    {
        if (!isTracking) return;
        isTracking = false;
        EnableFreeLook();
    }
    private void EnableFreeLook()
    {
        if (prioritySystem)
        {
            freeLookCamera.Priority = 20;
            trackingCamera.Priority = 10;
        }
        else
        {
            freeLookCamera.enabled = true;
            trackingCamera.enabled = false;
        }
        freeLookCamera.GetComponent<PlayerCameraController>().InitializeRotation();
    }

    private void EnableTrackingCamera()
    {
        if (prioritySystem)
        {
            freeLookCamera.Priority = 10;
            trackingCamera.Priority = 20;
        }
        else
        {
            freeLookCamera.enabled = false;
            trackingCamera.enabled = true;
        }
        GetComponent<PlayerDominoPlacement>().ReleaseDomino();
    }
}
