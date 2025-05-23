using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlowOutlineManager
{
    private Material glowOutlineMaterial;
    private Domino hoveredDomino;
    private Camera activeCamera;
    private Material instanceMaterial; // Add a field for the instance material

    public GlowOutlineManager(Material glowOutlineMaterial, Camera activeCamera)
    {
        this.glowOutlineMaterial = glowOutlineMaterial;
        this.activeCamera = activeCamera;
        this.instanceMaterial = new Material(glowOutlineMaterial); // Create an instance material
    }

    public void HandleMouseHover(GameObject heldDomino)
    {
        if (heldDomino != null || !PlayerDominoPlacement.Instance.ControlsActive())
        {
            RemoveGlowOutline();
            return;
        }

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Domino domino = hit.collider.GetComponent<Domino>();
            if (domino != null && domino.currentState != Domino.DominoState.Animating)
            {
                if (domino != hoveredDomino) ApplyGlowOutline(domino);
            }
            else RemoveGlowOutline();
        }
        else if (hoveredDomino != null)
        {
            RemoveGlowOutline();
        }
    }

    private void ApplyGlowOutline(Domino domino)
    {
        RemoveGlowOutline();

        hoveredDomino = domino;

        Transform glowOutlineTransform = domino.transform.Find("GlowOutline");
        if (glowOutlineTransform == null)
        {
            GameObject glowOutlineObject = new GameObject("GlowOutline");
            glowOutlineObject.transform.SetParent(domino.transform);
            glowOutlineObject.transform.localPosition = new Vector3(0, 0.01f, 0);
            glowOutlineObject.transform.localRotation = Quaternion.identity;
            glowOutlineObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = glowOutlineObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = glowOutlineObject.AddComponent<MeshRenderer>();

            MeshFilter originalMeshFilter = domino.GetComponentInChildren<MeshFilter>();
            if (originalMeshFilter != null)
            {
                meshFilter.sharedMesh = originalMeshFilter.sharedMesh;
            }

            meshRenderer.material = instanceMaterial; // Use the instance material
            glowOutlineTransform = glowOutlineObject.transform;
        }

        MeshRenderer glowRenderer = glowOutlineTransform.GetComponent<MeshRenderer>();
        if (glowRenderer != null)
        {
            glowRenderer.enabled = true;

            DominoSkin dominoSkin = domino.GetComponent<DominoSkin>();
            Color outlineColor = Color.blue;
            if (dominoSkin != null)
            {
                Color dominoColor = dominoSkin.colorOverride;
                if (dominoColor != Color.white)
                {
                    outlineColor = new Color(1f - dominoColor.r, 1f - dominoColor.g, 1f - dominoColor.b);
                }
            }

            Vector3 scaleVector = new Vector3(
                0.16f / domino.transform.localScale.x,
                0.06f / domino.transform.localScale.y,
                0.36f / domino.transform.localScale.z
            );

            instanceMaterial.SetColor("_Color", outlineColor); // Modify the instance material
            instanceMaterial.SetVector("_ScaleVector", Vector3.one + scaleVector);
        }
    }

    public void RemoveGlowOutline()
    {
        if (hoveredDomino == null) return;

        Transform glowOutlineTransform = hoveredDomino.transform.Find("GlowOutline");
        if (glowOutlineTransform != null)
        {
            MeshRenderer glowRenderer = glowOutlineTransform.GetComponent<MeshRenderer>();
            if (glowRenderer != null)
            {
                glowRenderer.enabled = false;
            }
        }

        hoveredDomino = null;
    }

    private bool IsCameraActive()
    {
        return activeCamera != null && activeCamera.enabled;
    }
}