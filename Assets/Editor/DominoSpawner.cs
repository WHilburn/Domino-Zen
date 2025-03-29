using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DominoSpawner : EditorWindow
{
    private enum FormationType { Line, Triangle, Curve, Spiral }
    private enum Direction { Left, Right, Both }
    
    private FormationType selectedFormation = FormationType.Line;
    private Direction curveDirection = Direction.Right;

    private int spawnCount = 5; // Total dominoes, or number of rows in triangle
    private float forwardSpacing = 0.4f;
    private float rowSpacing = 0.6f;
    private float curveAngle = 90f; // Angle of curve (5° - 360°)
    private float spiralSpacing = 0.3f; // Shrinking radius spacing for spiral

    private bool gradientMode = false;
    private Color startColor = Color.red;
    private Color endColor = Color.blue;
    private int colorCycles = 1;
    private bool colorBounce = false;
    private string groupName = "Domino Group";
    private GameObject dominoPrefab;
    private List<GameObject> previewCubes = new List<GameObject>(); // To store preview cubes

    [MenuItem("Tools/Domino Spawner")]
    public static void ShowWindow()
    {
        GetWindow<DominoSpawner>("Domino Spawner");
    }

    void OnGUI()
    {
        // Debug.Log("OnGUI called");
        GUILayout.Label("Domino Spawner", EditorStyles.boldLabel);
        // Allow the user to assign a domino prefab
        dominoPrefab = (GameObject)EditorGUILayout.ObjectField("Domino Prefab", dominoPrefab, typeof(GameObject), false);

        selectedFormation = (FormationType)EditorGUILayout.EnumPopup("Formation Type", selectedFormation);
        forwardSpacing = EditorGUILayout.Slider("Forward Spacing", forwardSpacing, .2f, .6f);

        if (selectedFormation == FormationType.Line)
        {
            spawnCount = EditorGUILayout.IntField("Domino Count", spawnCount);
            groupName = "Domino Line Length " + spawnCount;
        }
        else if (selectedFormation == FormationType.Triangle)
        {
            rowSpacing = EditorGUILayout.Slider("In-Row Spacing", rowSpacing, .5f, .8f);
            spawnCount = EditorGUILayout.IntField("Row Count", spawnCount);
            groupName = "Domino Triangle " + spawnCount + " Rows";
        }
        else if (selectedFormation == FormationType.Curve)
        {
            spawnCount = EditorGUILayout.IntField("Domino Count", spawnCount);
            curveAngle = EditorGUILayout.Slider("Curve Angle", curveAngle, 5f, 360f);
            curveDirection = (Direction)EditorGUILayout.EnumPopup("Direction", curveDirection);
            groupName = "Domino Curve Length " + spawnCount  + " " + curveAngle + "° " + curveDirection;
        }
        else if (selectedFormation == FormationType.Spiral)
        {
            spawnCount = EditorGUILayout.IntField("Domino Count", spawnCount);
            spiralSpacing = EditorGUILayout.FloatField("Spiral Spacing", spiralSpacing);
            curveDirection = (Direction)EditorGUILayout.EnumPopup("Direction", curveDirection);
            groupName = "Domino Spiral " + spawnCount + " Dominoes, " + curveDirection;
        }
        GeneratePreviewCubes(Selection.activeGameObject);

        gradientMode = EditorGUILayout.Toggle("Color Gradient Mode", gradientMode);

        if (gradientMode)
        {
            startColor = EditorGUILayout.ColorField("Start Color", startColor);
            endColor = EditorGUILayout.ColorField("End Color", endColor);
            colorCycles = EditorGUILayout.IntField("Color Cycles", colorCycles);
            if (colorCycles > 1)
            {
                colorBounce = EditorGUILayout.Toggle("Color Bounce", colorBounce);
            }
        }

        if (GUILayout.Button("Spawn Dominoes"))
        {
            SpawnDominoes();
        }
    }

    private void OnSelectionChange()
    {
        // Update the window when the selection changes
        Repaint();
        RemovePreviewCubes();
        GeneratePreviewCubes(Selection.activeGameObject);
    }
    private void OnDestroy()
    {
        RemovePreviewCubes(); // Clean up preview cubes when the window is closed
    }

    private void SpawnDominoes()
    {
        RemovePreviewCubes();
        
        if (dominoPrefab == null)
        {
            Debug.LogWarning("Please assign a domino prefab in the Domino Spawner window.");
            return;
        }

        GameObject selected = Selection.activeGameObject;
        if (selected == null || !selected.CompareTag("DominoTag"))
        {
            Debug.LogWarning("Please select a domino in the scene.");
            return;
        }

        List<(Vector3 position, Quaternion rotation)> spawnData = new List<(Vector3, Quaternion)>();

        switch (selectedFormation)
        {
            case FormationType.Line:
                spawnData = GetLineFormationPositions(selected);
                break;
            case FormationType.Triangle:
                spawnData = GetTriangleFormationPositions(selected);
                break;
            case FormationType.Curve:
                spawnData = GetCurveFormationPositions(selected);
                break;
            case FormationType.Spiral:
                spawnData = GetSpiralFormationPositions(selected);
                break;
            default:
                Debug.LogWarning("Unsupported formation type.");
                return;
        }

        if (spawnData == null || spawnData.Count == 0) return;

        // Create an empty GameObject as the parent
        GameObject parent = new GameObject(groupName);
        parent.transform.position = selected.transform.position;
        Undo.RegisterCreatedObjectUndo(parent, "Spawn Domino");

        List<GameObject> newDominoes = new List<GameObject>();

        // Instantiate dominoes at computed positions and rotations
        foreach (var (position, rotation) in spawnData)
        {
            GameObject newDomino = SpawnDominoPrefab(position, rotation);
            newDomino.transform.SetParent(parent.transform);
            newDominoes.Add(newDomino);
        }

        if (gradientMode)
        {
            ApplyColorGradient(newDominoes);
        }
    }


    private GameObject SpawnDominoPrefab(Vector3 spawnPos, Quaternion rotation)
    {
        GameObject newDomino = (GameObject)PrefabUtility.InstantiatePrefab(dominoPrefab);
        newDomino.transform.position = spawnPos;
        newDomino.transform.rotation = rotation;
        Undo.RegisterCreatedObjectUndo(newDomino, "Spawn Domino");
        return newDomino;
    }

    // Method to display preview cubes at spawn positions
    private void GeneratePreviewCubes(GameObject selected)
    {
        // Debug.Log("Generating Preview Cubes...");
        // Remove any existing preview cubes
        RemovePreviewCubes();

        if (selected == null || !selected.CompareTag("DominoTag") || spawnCount <= 0)
            return;

        List<(Vector3 position, Quaternion rotation)> previewPositions = new();

        switch (selectedFormation)
        {
            case FormationType.Line:
                previewPositions = GetLineFormationPositions(selected);
                break;
            case FormationType.Triangle:
                previewPositions = GetTriangleFormationPositions(selected);
                break;
            case FormationType.Curve:
                previewPositions = GetCurveFormationPositions(selected);
                break;
            case FormationType.Spiral:
                previewPositions = GetSpiralFormationPositions(selected);
                break;
            default:
                Debug.LogWarning("Unsupported formation type.");
                return;
        }

        foreach ((Vector3 pos, Quaternion rot) in previewPositions)
        {
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = pos;
            cube.transform.rotation = rot;
            cube.transform.localScale = new Vector3(.48f, .13f, .98f); // Match approximate domino shape
            cube.name = "PreviewCube";
            cube.hideFlags = HideFlags.HideAndDontSave; // Hide from hierarchy
            previewCubes.Add(cube);
        }
    }


    // Remove all preview cubes from the scene
    private void RemovePreviewCubes()
    {
        foreach (GameObject cube in previewCubes)
        {
            DestroyImmediate(cube);
        }
        previewCubes.Clear();
    }

    private List<(Vector3 position, Quaternion rotation)> GetLineFormationPositions(GameObject selected)
    {
        List<(Vector3, Quaternion)> spawnTransforms = new List<(Vector3, Quaternion)>();
        Vector3 startPos = selected.transform.position;
        Quaternion rotation = selected.transform.rotation;

        for (int i = 1; i <= spawnCount; i++)
        {
            Vector3 spawnPos = startPos + selected.transform.up * (forwardSpacing * i);
            spawnTransforms.Add((spawnPos, rotation)); // Store position and rotation as a tuple
        }
        // PreviewDominoPositions(spawnTransforms);
        return spawnTransforms;
    }

    private List<(Vector3 position, Quaternion rotation)> GetTriangleFormationPositions(GameObject selected)
    {
        List<(Vector3, Quaternion)> spawnTransforms = new List<(Vector3, Quaternion)>();
        Vector3 startPos = selected.transform.position + selected.transform.up * forwardSpacing;
        Quaternion rotation = selected.transform.rotation;

        int dominoesInRow = 2; // First row starts with 2 dominoes

        for (int currentRow = 0; currentRow < spawnCount; currentRow++)
        {
            for (int i = 0; i < dominoesInRow; i++)
            {
                float offsetX = (i - (dominoesInRow - 1) / 2f) * rowSpacing;
                float offsetZ = currentRow * forwardSpacing;
                Vector3 spawnPos = startPos + selected.transform.right * offsetX + selected.transform.up * offsetZ;

                spawnTransforms.Add((spawnPos, rotation)); // Store position and rotation
            }

            dominoesInRow++; // Each new row has one more domino than the last
        }
        // PreviewDominoPositions(spawnTransforms);
        return spawnTransforms;
    }

    private List<(Vector3 position, Quaternion rotation)> GetCurveFormationPositions(GameObject selected)
    {
        List<(Vector3, Quaternion)> spawnTransforms = new List<(Vector3, Quaternion)>();
        Vector3 startPos = selected.transform.position;
        Quaternion startRotation = selected.transform.rotation;

        float arcLength = spawnCount * forwardSpacing; // Total arc length
        float radius = arcLength / Mathf.Abs(curveAngle * Mathf.Deg2Rad); // Adjust radius based on desired curve angle
        float angleStep = curveAngle / spawnCount; // Angle change per domino

        void CalculateArc(Vector3 arcStartPos, float directionMultiplier)
        {
            Vector3 center = arcStartPos - selected.transform.right * directionMultiplier * radius; // Shift center to the side

            for (int i = 0; i < spawnCount; i++)
            {
                float angle = angleStep * i * directionMultiplier; // Angle relative to the starting position
                Quaternion newRotation = Quaternion.AngleAxis(angle, Vector3.up) * startRotation; // Rotate around Y-axis

                Vector3 offset = newRotation * Vector3.up * radius; // Correct offset direction
                Vector3 spawnPos = center + offset; // Final position

                spawnTransforms.Add((spawnPos, newRotation * Quaternion.Euler(0, 0, 90f * directionMultiplier)));
            }
        }
        // Calculate arc positions based on the selected direction
        switch (curveDirection)
        {
            case Direction.Left:
                CalculateArc(startPos, 1);
                break;
            case Direction.Right:
                CalculateArc(startPos, -1);
                break;
            case Direction.Both:
                CalculateArc(startPos + selected.transform.right * -0.25f, 1);
                CalculateArc(startPos + selected.transform.right * 0.25f, -1);
                break;
        }

        // PreviewDominoPositions(spawnTransforms);
        return spawnTransforms;
    }

    private List<(Vector3 position, Quaternion rotation)> GetSpiralFormationPositions(GameObject selected)
    {
        if (curveDirection == Direction.Both) spawnCount *= 2; // Double the count for both directions

        List<(Vector3, Quaternion)> spawnTransforms = new List<(Vector3, Quaternion)>();
        Vector3 startPos = selected.transform.position;
        Quaternion rotation = selected.transform.rotation;
        float angleStep = 360f / spawnCount;
        float radius = spawnCount * spiralSpacing;

        void CalculateSpiral(float directionMultiplier)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad * directionMultiplier;
                float currentRadius = radius - (spiralSpacing * i);
                Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * currentRadius;
                Vector3 spawnPos = startPos + selected.transform.right * offset.x + selected.transform.up * offset.z;
                Quaternion newRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * rotation;

                spawnTransforms.Add((spawnPos, newRotation));
            }
        }

        if (curveDirection == Direction.Left || curveDirection == Direction.Both)
            CalculateSpiral(1);
        if (curveDirection == Direction.Right || curveDirection == Direction.Both)
            CalculateSpiral(-1);

        if (curveDirection == Direction.Both) spawnCount /= 2; // Reset to original count
        // PreviewDominoPositions(spawnTransforms);
        return spawnTransforms;
    }

    private void ApplyColorGradient(List<GameObject> dominoes)
    {
        DominoSkin selectedSkin = Selection.activeGameObject.GetComponent<DominoSkin>();
        for (int i = 0; i < dominoes.Count; i++)
        {
            float cycleIndex = (i / (dominoes.Count / (float)colorCycles)) % 1f;
            bool reversed = false;
            if (colorBounce && ((i / (dominoes.Count / colorCycles)) % 2 == 1))
            {
                reversed = true;
            }

            Color newColor;
            if (reversed)
                newColor = Color.Lerp(endColor, startColor, cycleIndex);
            else
                newColor = Color.Lerp(startColor, endColor, cycleIndex);
            DominoSkin dominoSkin = dominoes[i].GetComponent<DominoSkin>();
            if (dominoSkin != null)
            {
                dominoSkin.materialList = selectedSkin.materialList;
                dominoSkin.colorOverride = newColor;
                dominoSkin.ApplyRandomMaterial();
                EditorUtility.SetDirty(dominoSkin);
            }
        }
    }
}
