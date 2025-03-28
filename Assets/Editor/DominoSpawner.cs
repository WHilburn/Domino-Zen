using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DominoSpawner : EditorWindow
{
    private enum FormationType { Line, Triangle, Curve, Spiral }
    private enum Direction { Left, Right, Both }
    
    private FormationType selectedFormation = FormationType.Line;
    private Direction curveDirection = Direction.Right;
    private Direction spiralDirection = Direction.Right;

    private int spawnCount = 5; // Total dominoes or rows in triangle
    private float forwardSpacing = 0.4f;
    private float rowSpacing = 0.6f;
    private float curveAngle = 90f; // Angle of curve (5° - 360°)
    private float spiralSpacing = 0.3f; // Shrinking radius spacing for spiral

    private bool gradientMode = false;
    private Color startColor = Color.red;
    private Color endColor = Color.blue;
    private int colorCycles = 1;
    private bool colorBounce = false;

    [MenuItem("Tools/Domino Spawner")]
    public static void ShowWindow()
    {
        GetWindow<DominoSpawner>("Domino Spawner");
    }

    void OnGUI()
    {
        GUILayout.Label("Domino Spawner", EditorStyles.boldLabel);

        selectedFormation = (FormationType)EditorGUILayout.EnumPopup("Formation Type", selectedFormation);
        forwardSpacing = EditorGUILayout.FloatField("Spacing", forwardSpacing);

        if (selectedFormation == FormationType.Line)
        {
            spawnCount = EditorGUILayout.IntField("Domino Count", spawnCount);
        }
        else if (selectedFormation == FormationType.Triangle)
        {
            rowSpacing = EditorGUILayout.FloatField("Row Spacing", rowSpacing);
            spawnCount = EditorGUILayout.IntField("Row Count", spawnCount);
        }
        else if (selectedFormation == FormationType.Curve)
        {
            spawnCount = EditorGUILayout.IntField("Domino Count", spawnCount);
            curveAngle = EditorGUILayout.Slider("Curve Angle", curveAngle, 5f, 360f);
            curveDirection = (Direction)EditorGUILayout.EnumPopup("Direction", curveDirection);
        }
        else if (selectedFormation == FormationType.Spiral)
        {
            spawnCount = EditorGUILayout.IntField("Domino Count", spawnCount);
            spiralSpacing = EditorGUILayout.FloatField("Spiral Spacing", spiralSpacing);
            spiralDirection = (Direction)EditorGUILayout.EnumPopup("Direction", spiralDirection);
        }

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

    private void SpawnDominoes()
    {
        GameObject selected = Selection.activeGameObject;
        if (selected == null || !selected.CompareTag("DominoTag"))
        {
            Debug.LogWarning("Please select a domino in the scene.");
            return;
        }

        List<GameObject> newDominoes = new List<GameObject>();

        switch (selectedFormation)
        {
            case FormationType.Line:
                newDominoes = SpawnLineFormation(selected);
                break;
            case FormationType.Triangle:
                newDominoes = SpawnTriangleFormation(selected);
                break;
            case FormationType.Curve:
                newDominoes = SpawnCurveFormation(selected);
                break;
            case FormationType.Spiral:
                newDominoes = SpawnSpiralFormation(selected);
                break;
            default:
                Debug.LogWarning("Unsupported formation type.");
                return;
        }

        if (gradientMode)
        {
            ApplyColorGradient(newDominoes);
        }
    }

        private List<GameObject> SpawnLineFormation(GameObject selected)
    {
        List<GameObject> dominoes = new List<GameObject>();
        Vector3 startPos = selected.transform.position;
        Quaternion rotation = selected.transform.rotation;

        for (int i = 1; i <= spawnCount; i++)
        {
            Vector3 spawnPos = startPos + selected.transform.up * (forwardSpacing * i);
            GameObject newDomino = Instantiate(selected, spawnPos, rotation);
            Undo.RegisterCreatedObjectUndo(newDomino, "Spawn Domino");
            dominoes.Add(newDomino);
        }

        return dominoes;
    }

    private List<GameObject> SpawnTriangleFormation(GameObject selected)
    {
        List<GameObject> dominoes = new List<GameObject>();
        Vector3 startPos = selected.transform.position + selected.transform.up * (forwardSpacing);
        Quaternion rotation = selected.transform.rotation;

        int dominoesInRow = 2; // First row starts with 2 dominoes

        for (int currentRow = 0; currentRow < spawnCount; currentRow++)
        {
            for (int i = 0; i < dominoesInRow; i++)
            {
                float offsetX = (i - (dominoesInRow - 1) / 2f) * rowSpacing;
                float offsetZ = currentRow * forwardSpacing;
                Vector3 spawnPos = startPos + selected.transform.right * offsetX + selected.transform.up * offsetZ;

                GameObject newDomino = Instantiate(selected, spawnPos, rotation);
                Undo.RegisterCreatedObjectUndo(newDomino, "Spawn Domino");
                dominoes.Add(newDomino);
            }

            dominoesInRow++; // Each new row has one more domino than the last
        }

        return dominoes;
    }

    private List<GameObject> SpawnCurveFormation(GameObject selected)
    {
        List<GameObject> dominoes = new List<GameObject>();
        Vector3 startPos = selected.transform.position;
        Quaternion rotation = selected.transform.rotation;
        float angleStep = curveAngle / (spawnCount - 1);
        float radius = spawnCount * forwardSpacing / Mathf.PI;

        void SpawnArc(float directionMultiplier)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad * directionMultiplier;
                Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * radius;
                Vector3 spawnPos = startPos + selected.transform.right * offset.x + selected.transform.up * offset.z;
                Quaternion newRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * rotation;

                GameObject newDomino = Instantiate(selected, spawnPos, newRotation);
                Undo.RegisterCreatedObjectUndo(newDomino, "Spawn Domino");
                dominoes.Add(newDomino);
            }
        }

        if (curveDirection == Direction.Left || curveDirection == Direction.Both)
            SpawnArc(1);
        if (curveDirection == Direction.Right || curveDirection == Direction.Both)
            SpawnArc(-1);

        return dominoes;
    }

    private List<GameObject> SpawnSpiralFormation(GameObject selected)
    {
        List<GameObject> dominoes = new List<GameObject>();
        Vector3 startPos = selected.transform.position;
        Quaternion rotation = selected.transform.rotation;
        float angleStep = 360f / spawnCount;
        float radius = spawnCount * spiralSpacing;

        void SpawnSpiral(float directionMultiplier)
        {
            for (int i = 0; i < spawnCount; i++)
            {
                float angle = angleStep * i * Mathf.Deg2Rad * directionMultiplier;
                float currentRadius = radius - (spiralSpacing * i);
                Vector3 offset = new Vector3(Mathf.Sin(angle), 0, Mathf.Cos(angle)) * currentRadius;
                Vector3 spawnPos = startPos + selected.transform.right * offset.x + selected.transform.up * offset.z;
                Quaternion newRotation = Quaternion.Euler(0, -angle * Mathf.Rad2Deg, 0) * rotation;

                GameObject newDomino = Instantiate(selected, spawnPos, newRotation);
                Undo.RegisterCreatedObjectUndo(newDomino, "Spawn Domino");
                dominoes.Add(newDomino);
            }
        }

        if (spiralDirection == Direction.Left || spiralDirection == Direction.Both)
            SpawnSpiral(1);
        if (spiralDirection == Direction.Right || spiralDirection == Direction.Both)
            SpawnSpiral(-1);

        return dominoes;
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
