using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DominoPainter : EditorWindow
{
    private DominoMaterialList selectedMaterialList;
    private Color selectedColor = Color.white; // Default color is white
    private GameObject indicatorPrefab; // Reference to the placement indicator prefab
    private GameObject dominoPrefab; // Reference to the placement indicator prefab
    private bool rainbowMode = false; // Toggle for Rainbow Mode

    [MenuItem("Tools/Domino Painter")]
    public static void ShowWindow()
    {
        GetWindow<DominoPainter>("Domino Painter");
    }

    void OnGUI()
    {
        GUILayout.Label("Domino Painter", EditorStyles.boldLabel);

        // Select a material list
        selectedMaterialList = (DominoMaterialList)EditorGUILayout.ObjectField("Material List", selectedMaterialList, typeof(DominoMaterialList), false);
        selectedColor = EditorGUILayout.ColorField("Start Color", selectedColor);

        // Select the prefab objects
        dominoPrefab = (GameObject)EditorGUILayout.ObjectField("Domino Prefab", dominoPrefab, typeof(GameObject), false);
        indicatorPrefab = (GameObject)EditorGUILayout.ObjectField("Indicator Prefab", indicatorPrefab, typeof(GameObject), false);

        rainbowMode = EditorGUILayout.Toggle("Rainbow Mode", rainbowMode);

        // Button to apply material to selected dominoes
        if (GUILayout.Button("Apply to Selected"))
        {
            ApplyMaterialToSelected();
        }

        // Button to replace dominoes with indicators
        if (GUILayout.Button("Replace with Indicators"))
        {
            ReplaceDominoesWithIndicators();
        }

        // Button to replace indicators with dominoes
        if (GUILayout.Button("Replace with Dominoes"))
        {
            ReplaceIndicatorsWithDominoes();
        }
    }

    private void ApplyMaterialToSelected()
    {
        // Get all selected objects in the Scene
        GameObject[] selectedObjects = Selection.gameObjects;

        List<DominoSkin> dominoSkinsToApply = new List<DominoSkin>();
        List<PlacementIndicator> placementSkinsToApply = new List<PlacementIndicator>();

        foreach (GameObject obj in selectedObjects)
        {
            // Look for DominoSkin components in selected objects and their children
            dominoSkinsToApply.AddRange(obj.GetComponentsInChildren<DominoSkin>());
            placementSkinsToApply.AddRange(obj.GetComponentsInChildren<PlacementIndicator>());
        }

        if (dominoSkinsToApply.Count + placementSkinsToApply.Count == 0)
        {
            Debug.LogWarning("No relevant objects found in the selected objects or their children.");
            return;
        }
        // Check if material list is assigned
        if (selectedMaterialList == null)
        {
            Debug.LogWarning("No material list selected!");
            return;
        }

        // Register the undo operation
        Undo.RegisterCompleteObjectUndo(dominoSkinsToApply.ToArray(), "Apply Material to Dominoes");
        Undo.RegisterCompleteObjectUndo(placementSkinsToApply.ToArray(), "Apply Material to Dominoes");
        Debug.Log($"Applying material list: {selectedMaterialList.name} to objects");

        // Apply the material and trigger ApplyRandomMaterial on each DominoSkin
        for (int i = 0; i < dominoSkinsToApply.Count; i++)
        {
            DominoSkin dominoSkin = dominoSkinsToApply[i];
            dominoSkin.materialList = selectedMaterialList; // Assign material list

            if (rainbowMode)
            {
                float hue = (i / (float)dominoSkinsToApply.Count); // Calculate hue based on index
                dominoSkin.colorOverride = Color.HSVToRGB(hue, 1f, 1f); // Apply rainbow color
            }
            else
            {
                dominoSkin.colorOverride = selectedColor; // Apply the selected color override
            }

            dominoSkin.ApplyRandomMaterial(); // Apply random material from the list
            EditorUtility.SetDirty(dominoSkin); // Mark the object as changed for saving
        }

        for (int i = 0; i < placementSkinsToApply.Count; i++)
        {
            PlacementIndicator placementSkin = placementSkinsToApply[i];

            if (rainbowMode)
            {
                float hue = (i / (float)placementSkinsToApply.Count); // Calculate hue based on index
                placementSkin.ApplyColor(Color.HSVToRGB(hue, 1f, 1f)); // Apply rainbow color
            }
            else
            {
                Color placementColor = placementSkin.indicatorColor; // Get the current color of the indicator
                if (selectedMaterialList.name == "BlackDominoMaterials")
                {
                    placementColor = Color.black;
                }
                placementSkin.ApplyColor(placementColor); // Apply the selected color override
            }

            EditorUtility.SetDirty(placementSkin); // Mark the object as changed for saving
        }
    }

    private void ReplaceDominoesWithIndicators()
    {
        // Check if the indicator prefab is assigned
        if (indicatorPrefab == null)
        {
            Debug.LogWarning("No indicator prefab assigned!");
            return;
        }

        // Get all selected objects in the Scene
        GameObject[] selectedObjects = Selection.gameObjects;
        List<DominoSkin> dominoSkinsToReplace = new List<DominoSkin>();

        foreach (GameObject obj in selectedObjects)
        {
            // Look for DominoSkin components in selected objects and their children
            dominoSkinsToReplace.AddRange(obj.GetComponentsInChildren<DominoSkin>());
        }

        if (dominoSkinsToReplace.Count == 0)
        {
            Debug.LogWarning("No DominoSkin objects found in the selected objects or their children.");
            return;
        }

        // Register the undo operation
        Undo.RegisterCompleteObjectUndo(this, "Replace Dominoes with Indicators");

        // Replace each domino with a placement indicator
        foreach (DominoSkin dominoSkin in dominoSkinsToReplace)
        {
            // Get the domino's position, rotation, and color
            Transform dominoTransform = dominoSkin.transform;
            Color dominoColor = dominoSkin.colorOverride;
            if (dominoSkin.materialList.name == "BlackDominoMaterials") 
            {
                dominoColor = Color.black;
            }

            // Spawn the placement indicator
            GameObject indicator = (GameObject)PrefabUtility.InstantiatePrefab(indicatorPrefab, dominoTransform.parent);
            Undo.RegisterCreatedObjectUndo(indicator, "Create Placement Indicator");

            indicator.transform.position = dominoTransform.position;
            indicator.transform.rotation = dominoTransform.rotation;

            // Set the indicator's color
            PlacementIndicator placementIndicator = indicator.GetComponent<PlacementIndicator>();
            if (placementIndicator != null)
            {
                placementIndicator.ApplyColor(dominoColor); // Apply the color to the indicator
            }

            // Register the domino for undo before destroying it
            Undo.DestroyObjectImmediate(dominoSkin.gameObject);
        }

        Debug.Log($"Replaced {dominoSkinsToReplace.Count} dominoes with placement indicators.");
    }

    private void ReplaceIndicatorsWithDominoes()
    {
        // Check if the domino prefab is assigned
        if (dominoPrefab == null)
        {
            Debug.LogWarning("No domino prefab assigned!");
            return;
        }
        // Get all selected objects in the Scene
        GameObject[] selectedObjects = Selection.gameObjects;

        List<PlacementIndicator> indicatorsToReplace = new List<PlacementIndicator>();

        foreach (GameObject obj in selectedObjects)
        {
            // Look for PlacementIndicator components in selected objects and their children
            indicatorsToReplace.AddRange(obj.GetComponentsInChildren<PlacementIndicator>());
        }

        if (indicatorsToReplace.Count == 0)
        {
            Debug.LogWarning("No PlacementIndicator objects found in the selected objects or their children.");
            return;
        }

        // Register the undo operation
        Undo.RegisterCompleteObjectUndo(this, "Replace Indicators with Dominoes");

        // Replace each indicator with a domino
        foreach (PlacementIndicator indicator in indicatorsToReplace)
        {
            // Get the indicator's position, rotation, and color
            Transform indicatorTransform = indicator.transform;
            Color indicatorColor = indicator.indicatorColor;

            // Create a new GameObject for the domino
            GameObject domino = (GameObject)PrefabUtility.InstantiatePrefab(dominoPrefab, indicatorTransform.parent);
            Undo.RegisterCreatedObjectUndo(domino, "Create Domino");

            domino.transform.position = indicatorTransform.position;
            domino.transform.rotation = indicatorTransform.rotation;
            domino.transform.parent = indicatorTransform.parent;

            // Add and configure the DominoSkin component
            DominoSkin dominoSkin = domino.AddComponent<DominoSkin>();
            if (selectedMaterialList != null) dominoSkin.materialList = selectedMaterialList; // Assign the selected material list
            dominoSkin.colorOverride = indicatorColor; // Apply the indicator's color
            dominoSkin.ApplyRandomMaterial(); // Apply a random material from the list

            // Register the indicator for undo before destroying it
            Undo.DestroyObjectImmediate(indicator.gameObject);
        }

        Debug.Log($"Replaced {indicatorsToReplace.Count} placement indicators with dominoes.");
    }
}