using UnityEngine;

[ExecuteInEditMode]
public class DominoSkin : MonoBehaviour
{
    public DominoMaterialList materialList; // Reference to the shared material list

    void Awake()
    {
        ApplyRandomMaterial();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        ApplyRandomMaterial();
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

        // Apply to all LOD models
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.sharedMaterial = chosenMaterial; // Use sharedMaterial so changes persist in edit mode
        }
    }
}
