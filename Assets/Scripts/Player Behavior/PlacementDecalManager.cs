using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlacementDecalManager
{
    private DecalProjector placementDecal;
    private Material placementDecalMaterial;
    private Material placementDecalMaterialRed;
    private Material dashedOutlineMaterial;
    private Vector3 decalSize;
    private Vector3 decalPivot;
    private float maxDistance;
    private Camera activeCamera;
    private Quaternion savedRotation;
    private GameObject dashedBorderObject; // Secondary object for dashed border
    public static Transform mouseWorldPosition;

    public PlacementDecalManager(Material defaultMaterial, Material redMaterial, Material dashedMaterial, Vector3 size, Vector3 pivot, float maxDist, Camera camera, Quaternion rotation)
    {
        placementDecalMaterial = defaultMaterial;
        placementDecalMaterialRed = redMaterial;
        dashedOutlineMaterial = dashedMaterial;
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
        placementDecal.material = placementDecalMaterial; // Use the default material
        placementDecal.size = decalSize;
        placementDecal.enabled = false; // Initially hidden
        placementDecal.pivot = decalPivot;

        // Create dashed border using 4 thin quads
        float borderThickness = 0.0075f; // Thickness of the border
        Vector3 borderScaleX = new Vector3(decalSize.x, borderThickness, 1f);
        Vector3 borderScaleZ = new Vector3(decalSize.z + (borderThickness * 2f), borderThickness, 1f);

        // Top border
        GameObject topBorder = GameObject.CreatePrimitive(PrimitiveType.Quad);
        topBorder.name = "TopBorder";
        MeshRenderer topBorderRenderer = topBorder.GetComponent<MeshRenderer>();
        topBorderRenderer.material = dashedOutlineMaterial;
        UnityEngine.Object.Destroy(topBorder.GetComponent<Collider>()); // Remove collider
        topBorder.transform.SetParent(decalObject.transform);
        topBorder.transform.localPosition = new Vector3(0, .005f, decalSize.z / 2 + borderThickness / 2);
        topBorder.transform.localScale = borderScaleX;
        topBorder.transform.localRotation = Quaternion.Euler(90, 0, 0);

        // Bottom border
        GameObject bottomBorder = GameObject.CreatePrimitive(PrimitiveType.Quad);
        bottomBorder.name = "BottomBorder";
        MeshRenderer bottomBorderRenderer = bottomBorder.GetComponent<MeshRenderer>();
        bottomBorderRenderer.material = dashedOutlineMaterial;
        UnityEngine.Object.Destroy(bottomBorder.GetComponent<Collider>()); // Remove collider
        bottomBorder.transform.SetParent(decalObject.transform);
        bottomBorder.transform.localPosition = new Vector3(0, .005f, -(decalSize.z / 2 + borderThickness / 2));
        bottomBorder.transform.localScale = borderScaleX;
        bottomBorder.transform.localRotation = Quaternion.Euler(90, 180, 0);

        // Left border
        GameObject leftBorder = GameObject.CreatePrimitive(PrimitiveType.Quad);
        leftBorder.name = "LeftBorder";
        MeshRenderer leftBorderRenderer = leftBorder.GetComponent<MeshRenderer>();
        leftBorderRenderer.material = dashedOutlineMaterial;
        leftBorderRenderer.material.SetFloat("_DashSize", dashedOutlineMaterial.GetFloat("_DashSize") * 3); // Triple dash size
        UnityEngine.Object.Destroy(leftBorder.GetComponent<Collider>()); // Remove collider
        leftBorder.transform.SetParent(decalObject.transform);
        leftBorder.transform.localPosition = new Vector3(-(decalSize.x / 2 + borderThickness / 2), .005f, 0);
        leftBorder.transform.localScale = borderScaleZ;
        leftBorder.transform.localRotation = Quaternion.Euler(90, 270, 0);

        // Right border
        GameObject rightBorder = GameObject.CreatePrimitive(PrimitiveType.Quad);
        rightBorder.name = "RightBorder";
        MeshRenderer rightBorderRenderer = rightBorder.GetComponent<MeshRenderer>();
        rightBorderRenderer.material = dashedOutlineMaterial;
        rightBorderRenderer.material.SetFloat("_DashSize", dashedOutlineMaterial.GetFloat("_DashSize") * 3); // Triple dash size
        UnityEngine.Object.Destroy(rightBorder.GetComponent<Collider>()); // Remove collider
        rightBorder.transform.SetParent(decalObject.transform);
        rightBorder.transform.localPosition = new Vector3(decalSize.x / 2 + borderThickness / 2, .005f, 0);
        rightBorder.transform.localScale = borderScaleZ;
        rightBorder.transform.localRotation = Quaternion.Euler(90, 90, 0);

        // Group all borders under a parent object for easier management
        dashedBorderObject = new GameObject("DashedBorder");
        dashedBorderObject.transform.SetParent(decalObject.transform);
        dashedBorderObject.transform.localPosition = Vector3.zero;
        dashedBorderObject.SetActive(false); // Initially hidden

        topBorder.transform.SetParent(dashedBorderObject.transform);
        bottomBorder.transform.SetParent(dashedBorderObject.transform);
        leftBorder.transform.SetParent(dashedBorderObject.transform);
        rightBorder.transform.SetParent(dashedBorderObject.transform);
    }

    public void UpdatePlacementDecal(bool placementEnabled, GameObject heldDomino, Quaternion rotation)
    {
        if (placementDecal == null) CreatePlacementDecal();
        if (!placementEnabled || !IsCameraActive())
        {
            placementDecal.enabled = false;
            dashedBorderObject.SetActive(false); // Hide border
            return;
        }
        
        int environmentLayerMask = LayerMask.GetMask("EnvironmentLayer");
        Vector3 targetPosition;
        Quaternion targetRotation = rotation;

        if (heldDomino != null)
        {
            // Follow the point directly under the held domino
            targetPosition = new Vector3(heldDomino.transform.position.x, 0, heldDomino.transform.position.z);

            // Raycast down to determine the y position
            Ray downRay = new Ray(heldDomino.transform.position, Vector3.down);
            if (Physics.Raycast(downRay, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
            {
                targetPosition.y = hit.point.y + 0.005f; // Add a small offset to avoid clipping
                targetRotation = Quaternion.Euler(0, heldDomino.transform.rotation.eulerAngles.y, 0); // Use only the y rotation of the held domino
            }
        }
        else
        {
            // Follow the cursor
            Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);

            if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
            {
                targetPosition = hit.point;

                if (Vector3.Distance(activeCamera.transform.position, targetPosition) > maxDistance)
                {
                    placementDecal.enabled = false;
                    dashedBorderObject.SetActive(false); // Hide border
                    return;
                }
            }
            else
            {
                placementDecal.enabled = false;
                dashedBorderObject.SetActive(false); // Hide border
                return;
            }
        }

        placementDecal.transform.position = targetPosition;
        placementDecal.transform.rotation = targetRotation;
        mouseWorldPosition = placementDecal.transform;
        placementDecal.enabled = heldDomino == null; // Enable if no domino is held
        dashedBorderObject.SetActive(true); // Show border
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

    public Vector3 GetDecalPosition()
    {
        return placementDecal != null && placementDecal.enabled ? placementDecal.transform.position : Vector3.zero;
    }
}