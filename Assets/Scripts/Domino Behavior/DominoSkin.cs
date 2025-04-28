using DG.Tweening;
using UnityEngine;

[ExecuteInEditMode]
public class DominoSkin : MonoBehaviour
{
    public DominoMaterialList materialList; // Reference to the shared material list
    public Color colorOverride = Color.white; // Selected color, but not applied until runtime
    private Material instanceMaterial;

    void Awake()
    {
        if (Application.isPlaying)
        {
            ApplyRandomMaterial();
        }
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Do not apply material in editor mode, just refresh the Scene view marker
        UnityEditor.SceneView.RepaintAll();
    }
#endif

    public void ApplyRandomMaterial()
    {
        if (materialList == null || materialList.materials.Length == 0)
        {
            Debug.LogWarning("No material list assigned or the list is empty!", gameObject);
            return;
        }

        // Choose a random material from the shared list
        Material chosenMaterial = materialList.materials[Random.Range(0, materialList.materials.Length)];

        // Create a new material instance so we don't modify shared materials
        instanceMaterial = new Material(chosenMaterial);
        instanceMaterial.color = colorOverride; // Apply the stored color override

        // Apply to all LOD models
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            // Get the current materials array
            Material[] materials = renderer.materials;

            // Ensure there is at least one material slot
            if (materials.Length > 0)
            {
                // Replace only the material in slot 0
                materials[0] = instanceMaterial;
                renderer.materials = materials; // Reassign the modified materials array
            }
        }
    }
}