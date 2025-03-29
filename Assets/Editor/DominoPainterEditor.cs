using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DominoPainter : EditorWindow
{
    private DominoMaterialList selectedMaterialList;
    private Color selectedColor = Color.white; // Default color is white
    private string[] predefinedColors = new string[] { "Red", "Green", "Blue", "Yellow", "Black", "White" };

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

        // Button to apply material to selected dominoes
        if (GUILayout.Button("Apply to Selected"))
        {
            ApplyMaterialToSelected();
        }
    }

    private void ApplyMaterialToSelected()
    {
        // Check if material list is assigned
        if (selectedMaterialList == null)
        {
            Debug.LogWarning("No material list selected!");
            return;
        }

        // Get all selected objects in the Scene
        GameObject[] selectedObjects = Selection.gameObjects;

        List<DominoSkin> dominoSkinsToApply = new List<DominoSkin>();

        foreach (GameObject obj in selectedObjects)
        {
            // Look for DominoSkin components in selected objects and their children
            dominoSkinsToApply.AddRange(obj.GetComponentsInChildren<DominoSkin>());
        }

        if (dominoSkinsToApply.Count == 0)
        {
            Debug.LogWarning("No DominoSkin objects found in the selected objects or their children.");
            return;
        }

        // Apply the material and trigger ApplyRandomMaterial on each DominoSkin
        foreach (DominoSkin dominoSkin in dominoSkinsToApply)
        {
            dominoSkin.materialList = selectedMaterialList; // Assign material list
            dominoSkin.colorOverride = selectedColor; // Apply the selected color override
            dominoSkin.ApplyRandomMaterial(); // Apply random material from the list
            EditorUtility.SetDirty(dominoSkin); // Mark the object as changed for saving
        }

        Debug.Log($"Applied {selectedMaterialList.name} and color {selectedColor} to {dominoSkinsToApply.Count} dominoes.");
    }
}
