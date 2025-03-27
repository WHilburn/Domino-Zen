using UnityEngine;
using UnityEditor;
using System.Collections.Generic;

public class DominoPainter : EditorWindow
{
    private DominoMaterialList selectedMaterialList;
    private Color selectedColor = Color.white; // Default color is white
    private string[] predefinedColors = new string[] { "Red", "Green", "Blue", "Yellow", "Black", "White" };
    private Color[] colorOptions = new Color[]
    {
        Color.red, Color.green, Color.blue, Color.yellow, Color.black, Color.white
    };

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

        // Add a dropdown to choose predefined color
        int selectedIndex = Mathf.Max(0, System.Array.FindIndex(colorOptions, color => color == selectedColor));
        selectedIndex = EditorGUILayout.Popup("Select Color", selectedIndex, predefinedColors);
        selectedColor = colorOptions[selectedIndex];

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
