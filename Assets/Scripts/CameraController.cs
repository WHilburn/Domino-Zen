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

    public float heightOffset = 2f; // Extra height to prevent low shots
    public float smoothTime = 0.5f; // Smoothing time for camera adjustments
    private Vector3 velocity = Vector3.zero; // Used for smooth dampening

    public Transform lookAtTarget; // Empty GameObject to control camera look direction

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
        if (isTracking) return; // Prevent re-triggering unnecessarily
        isTracking = true;

        // Add dominoes to target group with smooth weight adjustments
        targetGroup.m_Targets = new CinemachineTargetGroup.Target[fallingDominoes.Count];
        for (int i = 0; i < fallingDominoes.Count; i++)
        {
            targetGroup.m_Targets[i] = new CinemachineTargetGroup.Target
            {
                target = fallingDominoes[i],
                weight = 1f,
                radius = 5f // Adjust for better framing
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

    private void UpdateTargetGroup()
    {
        if (fallingDominoes.Count == 0) return;

        // Compute the smooth center position of the target group
        Vector3 targetPosition = Vector3.zero;
        foreach (Transform domino in fallingDominoes)
        {
            targetPosition += domino.position;
        }
        targetPosition /= fallingDominoes.Count;
        targetPosition.y += heightOffset; // Raise the center smoothly

        // Smoothly move the target group to prevent jitter
        targetGroup.transform.position = Vector3.SmoothDamp(targetGroup.transform.position, targetPosition, ref velocity, smoothTime);

        // Adjust look-at target to always be slightly above the center
        if (lookAtTarget != null)
        {
            lookAtTarget.position = targetGroup.transform.position + Vector3.up * heightOffset;
        }

        // If all dominoes stop falling, stop tracking
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

        // Make sure tracking camera looks at the adjusted target
        trackingCamera.LookAt = lookAtTarget;
    }
}
