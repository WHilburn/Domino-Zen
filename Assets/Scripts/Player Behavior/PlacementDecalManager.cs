using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlacementDecalManager
{
    private DecalProjector placementDecal;
    private Material placementDecalMaterial;
    private Material placementDecalMaterialRed;
    private Vector3 decalSize;
    private Vector3 decalPivot;
    private float maxDistance;
    private Camera activeCamera;
    private Quaternion savedRotation;

    public PlacementDecalManager(Material defaultMaterial, Material redMaterial, Vector3 size, Vector3 pivot, float maxDist, Camera camera, Quaternion rotation)
    {
        placementDecalMaterial = defaultMaterial;
        placementDecalMaterialRed = redMaterial;
        decalSize = size;
        decalPivot = pivot;
        maxDistance = maxDist;
        activeCamera = camera;
        savedRotation = rotation;
        CreatePlacementDecal();
    }

    private void CreatePlacementDecal()
    {
        GameObject decalObject = new GameObject("PlacementDecal");
        placementDecal = decalObject.AddComponent<DecalProjector>();
        placementDecal.material = placementDecalMaterial;
        placementDecal.size = decalSize;
        placementDecal.enabled = false; // Initially hidden
        placementDecal.pivot = decalPivot;
        placementDecal.material.SetColor("_BaseColor", Color.blue);
    }

    public void UpdatePlacementDecal(bool placementEnabled, GameObject heldDomino, Quaternion rotation)
    {
        if (placementDecal == null) CreatePlacementDecal();
        if (heldDomino != null || !placementEnabled || !IsCameraActive())
        {
            placementDecal.enabled = false;
            return;
        }

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int environmentLayerMask = LayerMask.GetMask("EnvironmentLayer");

        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
        {
            Vector3 mousePosition = hit.point;

            if (Vector3.Distance(activeCamera.transform.position, mousePosition) > maxDistance)
            {
                placementDecal.enabled = false;
                return;
            }

            placementDecal.transform.position = mousePosition;
            placementDecal.transform.rotation = rotation; // Apply the updated rotation
            placementDecal.enabled = true;
        }
        else
        {
            placementDecal.enabled = false;
        }
    }

    public Domino CheckForObstruction(GameObject heldDomino, Quaternion savedRotation, float hoverOffset)
    {
        if (heldDomino != null) return null; // No obstruction check if domino is held

        Vector3 checkPosition = GetMouseWorldPosition(hoverOffset);
        Collider[] colliders = Physics.OverlapBox(checkPosition, new Vector3(0.255f, 0.5f, 0.065f), savedRotation);
        foreach (Collider collider in colliders)
        {
            Domino existingDomino = collider.GetComponent<Domino>();
            if (existingDomino != null && existingDomino != heldDomino)
            {
                SetMaterialRed();
                return existingDomino; // Obstruction detected
            }
        }
        SetMaterialDefault();
        return null; // No obstruction detected
    }

    private Vector3 GetMouseWorldPosition(float hoverOffset)
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int environmentLayerMask = LayerMask.GetMask("EnvironmentLayer");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
        {
            Vector3 position = hit.point + Vector3.up * hoverOffset;

            // Prevent the hand from going further than maxDistance units from the camera
            if (Vector3.Distance(activeCamera.transform.position, position) > maxDistance)
            {
                position = activeCamera.transform.position + 
                           (position - activeCamera.transform.position).normalized * maxDistance;
            }

            return position;
        }
        return ray.origin + ray.direction * 5f;
    }

    public void SetMaterialRed()
    {
        if (placementDecal != null)
        {
            placementDecal.material = placementDecalMaterialRed;
        }
    }

    public void SetMaterialDefault()
    {
        if (placementDecal != null)
        {
            placementDecal.material = placementDecalMaterial;
        }
    }

    private bool IsCameraActive()
    {
        return activeCamera != null && activeCamera.enabled;
    }
}