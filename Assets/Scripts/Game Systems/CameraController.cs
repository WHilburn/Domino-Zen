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
    public CinemachineVirtualCamera trackingCamera1; // Auto-framing camera
    public CinemachineVirtualCamera trackingCamera2; // Secondary auto-framing camera
    public CinemachineTargetGroup targetGroup; // Group of falling dominoes
    public static UnityEvent OnFreeLookCameraEnabled = new();
    public static UnityEvent OnFreeLookCameraDisabled = new();
    public static bool isTracking = false;
    private Dictionary<Transform, float> dominoTimers = new(); // Tracks time remaining for each domino in the target group
    private const float dominoLifetime = .25f; // Time before domino is removed from the target group
    public HashSet<Transform> trackedDominoes = new(); // Tracks dominoes already added to the target group
    public int minDominoesToTriggerTracking = 5; // Minimum dominoes to trigger tracking camera
    public static float switchBackDelay = 1f; // Delay before switching back to free look camera
    private bool readyToSwitch = false;
    private CinemachineVirtualCamera activeTrackingCamera; // Tracks the currently active tracking camera
    private CinemachineVirtualCamera inactiveTrackingCamera; // Tracks the inactive tracking camera
    public float obstructionCheckDuration = 0.25f; // Time before teleporting the camera
    private float obstructionTimer = 0f; // Tracks how long the raycast is obstructed
    public float minTeleportDistance = 2f; // Minimum distance to allow teleporting
    public float teleportCooldown = 5f; // Cooldown time between teleports
    private float lastTeleportTime; // Tracks the last teleport time
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
        var trackedDolly = trackingCamera1.GetCinemachineComponent<CinemachineTrackedDolly>();
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

        // Initialize active and inactive tracking cameras
        activeTrackingCamera = trackingCamera1;
        inactiveTrackingCamera = trackingCamera2;
        lastTeleportTime = -teleportCooldown;
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
        CheckForObstruction(); // Check for obstructions between the camera and the target group

        if (dominoTimers.Count >= minDominoesToTriggerTracking && !isTracking)
        {
            EnableTrackingCamera();
            Debug.Log($"Tracking camera enabled with {dominoTimers.Count} dominoes.");
        }
        else if (readyToSwitch && isTracking)
        {
            readyToSwitch = false; // Reset the flag
            Invoke(nameof(EnableFreeLook), switchBackDelay); // Delay before switching to free look
            Debug.Log("Free look camera enabled, no dominoes in target group.");
            isTracking = false; // Reset tracking state
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
        trackingCamera1.Priority = 10;
        freeLookCamera.GetComponent<PlayerCameraController>().InitializeRotation();
        OnFreeLookCameraEnabled.Invoke();
        trackedDominoes.Clear(); // Allow dominoes to be tracked again
        dominoTimers.Clear(); // Clear the timers for all dominoes
        readyToSwitch = false; // Set the flag to switch back to free look camera
        foreach (var member in new List<CinemachineTargetGroup.Target>(targetGroup.m_Targets))
        {
            if (member.target != null)
            {
                targetGroup.RemoveMember(member.target);
            }
        }
    }

    private void EnableTrackingCamera()
    {
        isTracking = true;
        freeLookCamera.Priority = 10;
        trackingCamera1.Priority = 20;
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
                if (dominoTimers.Count == 1) {
                    readyToSwitch = true; // Set the flag to switch back to free look camera without removing the last domino from the tracking group
                    targetGroup.m_Targets[targetGroup.FindMember(domino)].weight = 1;
                }
                else RemoveDominoFromTargetGroup(domino);
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

    #region Obstruction Management
    private void CheckForObstruction()
    {
        if (!isTracking || targetGroup == null) return;

        Vector3 cameraPosition = trackingCamera1.transform.position;
        Vector3 targetGroupPosition = targetGroup.transform.position;
        Vector3 direction = targetGroupPosition - cameraPosition;

        if (Physics.Raycast(cameraPosition, direction, out RaycastHit hit, direction.magnitude, LayerMask.GetMask("EnvironmentLayer")))
        {
            obstructionTimer += Time.deltaTime;
            if (obstructionTimer > obstructionCheckDuration)
            {
                TeleportTrackingCamera();
                obstructionTimer = 0f; // Reset the timer after teleporting
            }
            Debug.DrawLine(cameraPosition, targetGroupPosition, Color.red,.2f);
        }
        else
        {
            Debug.DrawLine(cameraPosition, targetGroupPosition, Color.green,.2f);
            obstructionTimer = 0f; // Reset the timer if no obstruction
        }
    }

    private void TeleportTrackingCamera()
    {
        if (Time.time - lastTeleportTime < teleportCooldown) return; // Check cooldown

        var trackedDolly = inactiveTrackingCamera.GetCinemachineComponent<CinemachineTrackedDolly>();
        if (trackedDolly == null || trackedDolly.m_Path == null) return;

        Vector3 targetGroupPosition = targetGroup.transform.position;
        float closestDistance = float.MaxValue;
        float closestPosition = 0f;

        // Find the closest point on the dolly track to the target group
        for (float t = 0f; t <= trackedDolly.m_Path.PathLength; t += 0.1f)
        {
            Vector3 dollyPoint = trackedDolly.m_Path.EvaluatePositionAtUnit(t, CinemachinePathBase.PositionUnits.Distance);
            float distance = Vector3.Distance(dollyPoint, targetGroupPosition);
            if (distance < closestDistance)
            {
                closestDistance = distance;
                closestPosition = t;
            }
        }

        // Cancel teleporting if the closest distance is less than the minimum distance
        if (closestDistance < minTeleportDistance) return;

        // Set the dolly's path position to the closest point
        trackedDolly.m_PathPosition = closestPosition;

        // Synchronize the inactive camera's transform with the dolly's position and rotation
        inactiveTrackingCamera.transform.position = trackedDolly.m_Path.EvaluatePositionAtUnit(closestPosition, CinemachinePathBase.PositionUnits.Distance);
        inactiveTrackingCamera.transform.rotation = Quaternion.LookRotation(
            trackedDolly.m_Path.EvaluateTangentAtUnit(closestPosition, CinemachinePathBase.PositionUnits.Distance),
            Vector3.up
        );

        // Ensure the inactive camera is aimed at the target group
        inactiveTrackingCamera.transform.LookAt(targetGroup.transform);

        // Swap active and inactive cameras
        SwapTrackingCameras();

        lastTeleportTime = Time.time; // Update the last teleport time
        Debug.Log("Switched to tracking camera: " + activeTrackingCamera.name);
    }

    private void SwapTrackingCameras()
    {
        // Set the priority to swap active and inactive cameras
        activeTrackingCamera.Priority = 10;
        inactiveTrackingCamera.Priority = 20;

        // Swap references
        var temp = activeTrackingCamera;
        activeTrackingCamera = inactiveTrackingCamera;
        inactiveTrackingCamera = temp;
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
