using UnityEngine;
using UnityEditor;

[ExecuteInEditMode]
public class SceneObjectDebugger : MonoBehaviour
{
#if UNITY_EDITOR
    private int dominoCount = 0;
    private int indicatorCount = 0;
    private float nextUpdateTime = 0f;
    private float updateInterval = 0.5f; // Update every 0.5 seconds

    void OnEnable()
    {
        SceneView.duringSceneGui += OnSceneGUI;
    }

    void OnDisable()
    {
        SceneView.duringSceneGui -= OnSceneGUI;
    }

    void OnSceneGUI(SceneView sceneView)
    {
        if (Time.realtimeSinceStartup >= nextUpdateTime)
        {
            nextUpdateTime = Time.realtimeSinceStartup + updateInterval;
            dominoCount = GameObject.FindGameObjectsWithTag("DominoTag").Length;
            indicatorCount = GameObject.FindGameObjectsWithTag("IndicatorTag").Length;
        }

        Handles.BeginGUI();
        GUILayout.BeginArea(new Rect(50, 10, 300, 50)); // Adjusted size for two lines
        GUILayout.Label($"Active Dominoes: {dominoCount}", EditorStyles.boldLabel);
        GUILayout.Label($"Active Indicators: {indicatorCount}", EditorStyles.boldLabel);
        GUILayout.EndArea();
        Handles.EndGUI();
    }
#endif
}
