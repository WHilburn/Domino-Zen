using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DominoSpawner : EditorWindow
{
    private enum FormationType { Line, Triangle }
    private FormationType selectedFormation = FormationType.Line;
    private int spawnCount = 5;
    private float spacing = .4f;
    private bool rainbowMode = false;
    private Color startColor = Color.red;
    private Color endColor = Color.blue;
    private int colorCycles = 1;

    [MenuItem("Tools/Domino Spawner")]
    public static void ShowWindow()
    {
        GetWindow<DominoSpawner>("Domino Spawner");
    }

    void OnGUI()
    {
        GUILayout.Label("Domino Spawner", EditorStyles.boldLabel);

        selectedFormation = (FormationType)EditorGUILayout.EnumPopup("Formation Type", selectedFormation);
        spawnCount = EditorGUILayout.IntField("Spawn Count", spawnCount);
        spacing = EditorGUILayout.FloatField("Spacing", spacing);
        rainbowMode = EditorGUILayout.Toggle("Rainbow Mode", rainbowMode);

        if (rainbowMode)
        {
            startColor = EditorGUILayout.ColorField("Start Color", startColor);
            endColor = EditorGUILayout.ColorField("End Color", endColor);
            colorCycles = EditorGUILayout.IntField("Color Cycles", colorCycles);
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

        Vector3 startPos = selected.transform.position;
        Quaternion rotation = selected.transform.rotation;
        List<GameObject> newDominoes = new List<GameObject>();

        for (int i = 1; i <= spawnCount; i++)
        {
            Vector3 spawnPos = startPos;
            
            if (selectedFormation == FormationType.Line)
            {
                spawnPos += selected.transform.up * (spacing * i);
            }
            else if (selectedFormation == FormationType.Triangle)
            {
                int row = Mathf.FloorToInt(Mathf.Sqrt(2 * i));
                int indexInRow = i - (row * (row - 1) / 2);
                float offsetX = (indexInRow - row / 2f) * spacing;
                float offsetZ = row * spacing;
                spawnPos += selected.transform.right * offsetX + selected.transform.up * offsetZ;
            }
            
            GameObject newDomino = Instantiate(selected, spawnPos, rotation);
            Undo.RegisterCreatedObjectUndo(newDomino, "Spawn Domino");
            newDominoes.Add(newDomino);
        }

        if (rainbowMode)
        {
            ApplyRainbowColors(newDominoes);
        }
    }

    private void ApplyRainbowColors(List<GameObject> dominoes)
    {
        DominoSkin selectedSkin = Selection.activeGameObject.GetComponent<DominoSkin>();
        for (int i = 0; i < dominoes.Count; i++)
        {
            float t = ((float)(i % (dominoes.Count / colorCycles))) / (dominoes.Count / colorCycles);
            Color newColor = Color.Lerp(startColor, endColor, t);
            DominoSkin dominoSkin = dominoes[i].GetComponent<DominoSkin>();
            if (dominoSkin != null)
            {
                dominoSkin.materialList = selectedSkin.materialList; // Assign material list
                dominoSkin.colorOverride = newColor; // Apply the selected color override
                dominoSkin.ApplyRandomMaterial(); // Apply random material from the list
                EditorUtility.SetDirty(dominoSkin); // Mark the object as changed for saving
            }
        }
    }
}
