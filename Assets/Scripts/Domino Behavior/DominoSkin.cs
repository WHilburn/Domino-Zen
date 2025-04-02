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
        ApplyRandomMaterial();
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
            renderer.material = instanceMaterial; // Assign instance to avoid modifying sharedMaterial
        }
    }
    public void TweenColor(Color targetColor, float duration)
    {
        if (instanceMaterial == null)
        {
            Debug.LogWarning("Material instance is not initialized. Ensure ApplyRandomMaterial() is called first.");
            return;
        }

        // Use DOTween to tween the material's color to the target color
        instanceMaterial.DOColor(targetColor, duration).OnComplete(() =>
        {
            // Update the colorOverride to match the new color after the tween
            colorOverride = targetColor;
        });
    }
}