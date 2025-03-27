using UnityEngine;

[ExecuteInEditMode]
public class DominoSkin : MonoBehaviour
{
    public DominoMaterialList materialList; // Reference to the shared material list
    public Color colorOverride = Color.white; // Selected color, but not applied until runtime

    void Awake()
    {
        ApplyRandomMaterial();
    }

#if UNITY_EDITOR
    void OnValidate()
    {
        // Do not apply material in editor mode, just refresh the Scene view marker
        UnityEditor.SceneView.RepaintAll();
    }

    // void OnDrawGizmos()
    // {
    //     // Draw a small floating marker above the domino to indicate its selected color
    //     Vector3 markerPosition = transform.position + Vector3.up * 0.2f; // Slightly above the object
    //     Gizmos.color = colorOverride;
    //     Gizmos.DrawSphere(markerPosition, 0.05f);
    // }
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
        Material newMaterial = new Material(chosenMaterial);
        newMaterial.color = colorOverride; // Apply the stored color override

        // Apply to all LOD models
        MeshRenderer[] meshRenderers = GetComponentsInChildren<MeshRenderer>();
        foreach (MeshRenderer renderer in meshRenderers)
        {
            renderer.material = newMaterial; // Assign instance to avoid modifying sharedMaterial
        }
    }
}
