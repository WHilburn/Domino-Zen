using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DominoPainter : EditorWindow
{
    private DominoMaterialList selectedMaterialList;
    private Color selectedColor = Color.white; // Default color is white
    private GameObject indicatorPrefab; // Reference to the placement indicator prefab

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

        // Select the placement indicator prefab
        indicatorPrefab = (GameObject)EditorGUILayout.ObjectField("Indicator Prefab", indicatorPrefab, typeof(GameObject), false);

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
        if (dominoSkinsToApply.Count > 0 && selectedMaterialList == null)
        {
            Debug.LogWarning("No material list selected!");
            return;
        }

        // Register the undo operation
        Undo.RegisterCompleteObjectUndo(dominoSkinsToApply.ToArray(), "Apply Material to Dominoes");
        Undo.RegisterCompleteObjectUndo(placementSkinsToApply.ToArray(), "Apply Material to Dominoes");

        // Apply the material and trigger ApplyRandomMaterial on each DominoSkin
        foreach (DominoSkin dominoSkin in dominoSkinsToApply)
        {
            dominoSkin.materialList = selectedMaterialList; // Assign material list
            dominoSkin.colorOverride = selectedColor; // Apply the selected color override
            dominoSkin.ApplyRandomMaterial(); // Apply random material from the list
            EditorUtility.SetDirty(dominoSkin); // Mark the object as changed for saving
        }
        foreach (PlacementIndicator placementSkin in placementSkinsToApply)
        {
            placementSkin.ApplyColor(selectedColor); // Apply the selected color override
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
}