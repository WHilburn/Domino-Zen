using UnityEngine;
using UnityEditor;
using System.Collections.Generic;
using System.Reflection;
using System;
using System.Runtime.CompilerServices;

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
    private DominoMaterialList dominoMaterialList;
    private bool musicMode = true;
    private List<GameObject> previewShapes = new List<GameObject>(); // To store preview cubes
    private bool previewMode  = true;

    [MenuItem("Tools/Domino Spawner")]
    public static void ShowWindow()
    {
        GetWindow<DominoSpawner>("Domino Spawner");
    }

    void OnGUI()
    {
        // Debug.Log("OnGUI called");
        GUILayout.Label("Domino Spawner", EditorStyles.boldLabel);
        // Allow the user to assign a domino prefab and material list
        previewMode = EditorGUILayout.Toggle("Preview Mode", previewMode);
        dominoPrefab = (GameObject)EditorGUILayout.ObjectField("Domino Prefab", dominoPrefab, typeof(GameObject), false);
        dominoMaterialList = (DominoMaterialList)EditorGUILayout.ObjectField("Domino Material", dominoMaterialList, typeof(DominoMaterialList), false);
        musicMode = EditorGUILayout.Toggle("Music Mode", musicMode);
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
        else
        {
            startColor = EditorGUILayout.ColorField("Color", startColor);
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
    // When the game starts, remove all preview cubes
    private void OnEnable()
    {
        RemovePreviewCubes();
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

        ApplyColor(newDominoes);
    }


    private GameObject SpawnDominoPrefab(Vector3 spawnPos, Quaternion rotation)
    {
        GameObject newDomino = (GameObject)PrefabUtility.InstantiatePrefab(dominoPrefab);
        newDomino.transform.position = spawnPos;
        newDomino.GetComponent<DominoSound>().musicMode = musicMode;
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

        if (!previewMode || selected == null || !selected.CompareTag("DominoTag") || spawnCount <= 0)
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
            previewShapes.Add(cube);
        }
    }


    // Remove all preview cubes from the scene
    private void RemovePreviewCubes()
    {
        foreach (GameObject cube in previewShapes)
        {
            DestroyImmediate(cube);
        }
        previewShapes.Clear();
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

        // Compute arc length and radius
        float arcLength = spawnCount * forwardSpacing; 
        float radius = arcLength / Mathf.Abs(curveAngle * Mathf.Deg2Rad); 
        float angleStep = curveAngle / (spawnCount); // Ensure even spacing

        void CalculateArc(Vector3 arcStartPos, float directionMultiplier)
        {
            // Compute the center of the arc
            Vector3 center = arcStartPos - selected.transform.right * directionMultiplier * radius;

            // Debug sphere to visualize the center
            GameObject sphere = GameObject.CreatePrimitive(PrimitiveType.Sphere);
            sphere.transform.position = center;
            sphere.transform.localScale = new Vector3(0.5f, 0.5f, 0.5f);
            sphere.hideFlags = HideFlags.HideAndDontSave;
            previewShapes.Add(sphere);

            // Angle between the selected domino and the center
            float startAngleOffset = Mathf.Atan2(arcStartPos.z - center.z, arcStartPos.x - center.x) * Mathf.Rad2Deg;

            for (int i = 1; i < spawnCount + 1; i++)
            {
                // Compute angle for this domino
                float angle = startAngleOffset + (angleStep * i * directionMultiplier);
                float radian = angle * Mathf.Deg2Rad;

                // Compute position of the domino along the arc
                Vector3 spawnPos = center + new Vector3(Mathf.Cos(radian), 0, Mathf.Sin(radian)) * radius;

                // Compute the rotation to progressively curve along the arc
                Quaternion newRotation = Quaternion.Euler(0, angleStep * i * -directionMultiplier, 0) * startRotation;

                // Adjust rotation so the domino is upright and follows the curve
                spawnTransforms.Add((spawnPos, newRotation * Quaternion.Euler(0, 0, 0f * directionMultiplier)));
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

    private void ApplyColor(List<GameObject> dominoes)
    {
        DominoSkin selectedSkin = Selection.activeGameObject.GetComponent<DominoSkin>();
        DominoSound selectedSound = Selection.activeGameObject.GetComponent<DominoSound>();

        int halfCount = dominoes.Count / 2; // Halfway point for Both-direction handling
        Color effectiveEndColor = endColor;

        if (!gradientMode) effectiveEndColor = startColor;

        for (int i = 0; i < dominoes.Count; i++)
        {
            float cycleIndex;
            bool reversed = false;

            if (curveDirection == Direction.Both && (selectedFormation == FormationType.Curve || selectedFormation == FormationType.Spiral))
            {
                // Split into two halves for color calculation
                int localIndex = (i < halfCount) ? i : i - halfCount;
                int localCount = halfCount; // Each side should have its own independent gradient range

                cycleIndex = (localIndex / (float)localCount) % 1f;

                // Reverse coloring if needed for color bouncing
                if (colorBounce && ((localIndex / (localCount / colorCycles)) % 2 == 1))
                    reversed = true;
            }
            else
            {
                // Default case for single-direction coloring
                cycleIndex = (i / (dominoes.Count / (float)colorCycles)) % 1f;

                if (colorBounce && ((i / (dominoes.Count / colorCycles)) % 2 == 1))
                    reversed = true;
            }

            Color newColor = reversed ? Color.Lerp(effectiveEndColor, startColor, cycleIndex) 
                                    : Color.Lerp(startColor, effectiveEndColor, cycleIndex);

            DominoSkin dominoSkin = dominoes[i].GetComponent<DominoSkin>();
            if (dominoSkin != null)
            {
                if (dominoMaterialList == null)
                {
                    dominoSkin.materialList = selectedSkin.materialList; //Default to selected skin's material list
                }
                else dominoSkin.materialList = dominoMaterialList;
                dominoSkin.colorOverride = newColor;
                dominoSkin.ApplyRandomMaterial();
                EditorUtility.SetDirty(dominoSkin);
            }
        }
    }

}
