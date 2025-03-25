using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Cinemachine;

public class CameraController : MonoBehaviour
{
    public CinemachineFreeLook freeLookCamera; // Player-controlled camera
    public CinemachineVirtualCamera trackingCamera; // Auto-framing camera
    public CinemachineTargetGroup targetGroup; // Group of falling dominoes

    private bool isTracking = false;
    public List<Transform> fallingDominoes = new List<Transform>();

    void Start()
    {
        EnableFreeLook(); // Start with player control
    }

    void Update()
    {
        if (fallingDominoes.Count >= 2)
        {
            StartTracking();
        }
        else
        {
            StopTracking();
        }
        if (isTracking)
        {
            UpdateTargetGroup();
        }
    }

    public void StartTracking()
    {
        isTracking = true;

        // Add dominoes to target group
        targetGroup.m_Targets = new CinemachineTargetGroup.Target[fallingDominoes.Count];
        for (int i = 0; i < fallingDominoes.Count; i++)
        {
            targetGroup.m_Targets[i].target = fallingDominoes[i];
            targetGroup.m_Targets[i].weight = 1f;
            targetGroup.m_Targets[i].radius = 5f; // Adjust for better framing
        }

        EnableTrackingCamera();
    }

    public void StopTracking()
    {
        isTracking = false;
        EnableFreeLook();
    }

    private void UpdateTargetGroup()
    {
        if (fallingDominoes.Count == 0) return;



        if (fallingDominoes.Count == 0)
        {
            StopTracking();
        }
    }

    private void EnableFreeLook()
    {
        freeLookCamera.Priority = 10;
        trackingCamera.Priority = 5;
        
    }

    private void EnableTrackingCamera()
    {
        freeLookCamera.Priority = 5;
        trackingCamera.Priority = 20; // Higher priority takes over
    }
}
