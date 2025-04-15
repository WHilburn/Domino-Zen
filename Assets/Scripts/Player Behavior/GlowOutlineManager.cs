using UnityEngine;
using UnityEngine.Rendering.Universal;

public class GlowOutlineManager
{
    private Material glowOutlineMaterial;
    private Domino hoveredDomino;
    private Camera activeCamera;

    public GlowOutlineManager(Material glowOutlineMaterial, Camera activeCamera)
    {
        this.glowOutlineMaterial = glowOutlineMaterial;
        this.activeCamera = activeCamera;
    }

    public void HandleMouseHover(GameObject heldDomino)
    {
        if (heldDomino != null || !IsCameraActive())
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
            glowOutlineObject.transform.localPosition = Vector3.zero;
            glowOutlineObject.transform.localRotation = Quaternion.identity;
            glowOutlineObject.transform.localScale = Vector3.one;

            MeshFilter meshFilter = glowOutlineObject.AddComponent<MeshFilter>();
            MeshRenderer meshRenderer = glowOutlineObject.AddComponent<MeshRenderer>();

            MeshFilter originalMeshFilter = domino.GetComponentInChildren<MeshFilter>();
            if (originalMeshFilter != null)
            {
                meshFilter.sharedMesh = originalMeshFilter.sharedMesh;
            }

            meshRenderer.material = glowOutlineMaterial;
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

            glowOutlineMaterial.SetFloat("_Scale", 1.06f);
            glowOutlineMaterial.SetColor("_Color", outlineColor);
        }
    }

    private void RemoveGlowOutline()
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