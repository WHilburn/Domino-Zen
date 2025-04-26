using UnityEngine;
using System.Diagnostics;

public class DistancePerformanceTest : MonoBehaviour
{
    public PlacementIndicator[] indicators; // Array of placement indicators
    public Transform cameraTransform;

    void Start()
    {
        indicators = FindObjectsOfType<PlacementIndicator>();
        cameraTransform = PlayerDominoPlacement.Instance.activeCamera.transform;
        MeasurePerformance();
    }

    void MeasurePerformance()
    {
        Stopwatch stopwatch = new Stopwatch();

        // Test Manhattan distance
        stopwatch.Start();
        for (int i = 0; i < 1000; i++) // Simulate many calculations
        {
            foreach (var indicator in indicators)
            {
                float manhattanDistance = Mathf.Abs(indicator.transform.position.x - cameraTransform.position.x) +
                                          Mathf.Abs(indicator.transform.position.y - cameraTransform.position.y) +
                                          Mathf.Abs(indicator.transform.position.z - cameraTransform.position.z);
            }
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Manhattan Distance Time: " + stopwatch.ElapsedMilliseconds + " ms");

        // Test Euclidean distance
        stopwatch.Reset();
        stopwatch.Start();
        for (int i = 0; i < 1000; i++) // Simulate many calculations
        {
            foreach (var indicator in indicators)
            {
                float euclideanDistance = Vector3.Distance(indicator.transform.position, cameraTransform.position);
            }
        }
        stopwatch.Stop();
        UnityEngine.Debug.Log("Euclidean Distance Time: " + stopwatch.ElapsedMilliseconds + " ms");
    }
}