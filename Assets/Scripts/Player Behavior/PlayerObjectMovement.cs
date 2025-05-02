using System.Collections;
using UnityEngine;

public class PlayerObjectMovement : MonoBehaviour
{
    public static PlayerObjectMovement Instance { get; private set; }
    public static bool isMovingObject = false;
    private static GameObject selectedObject;
    private Camera activeCamera;
    private GameObject relocationIndicator; // Transparent cylinder for placement visualization
    private OverlapMesh overlapMesh; // Reference to the OverlapMesh script

    public void Initialize(Camera camera)
    {
        activeCamera = camera;
    }

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        // Create the placement indicator and disable it initially
        StartCoroutine(CreatePlacementIndicator());
    }

    private IEnumerator CreatePlacementIndicator()
    {
        while (SceneLoader.asyncLoad != null)
        {
            yield return null; // Wait for the scene to load
        }
        if (relocationIndicator == null)
        {
            relocationIndicator = Instantiate(PlayerDominoPlacement.Instance.cylinderPrefab, Vector3.zero, Quaternion.identity);
            overlapMesh = relocationIndicator.GetComponent<OverlapMesh>();
        }
        relocationIndicator.SetActive(false); // Hide the indicator initially
        yield return null;
    }

    void Update()
    {
        if (isMovingObject)
        {
            HandleObjectMovementMode();
        }
        else if (Input.GetButtonDown("Interact"))
        {
            TryEnterObjectMovementMode();
        }
    }

    private void TryEnterObjectMovementMode()
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit) && hit.collider.CompareTag("Bucket"))
        {
            selectedObject = hit.collider.gameObject;
            isMovingObject = true;
            PlayerDominoPlacement.Instance.TogglePlacementControls(false);
        }
    }

    private void HandleObjectMovementMode()
    {
        if (Input.GetKeyDown(KeyCode.Escape))
        {
            ExitObjectMovementMode();
            return;
        }

        Vector3 targetPosition = GetMouseWorldPosition();
        relocationIndicator.SetActive(true);
        relocationIndicator.transform.position = targetPosition + Vector3.up * 0.55f; // Adjust the indicator position

        if (!overlapMesh.isOverlapping && Input.GetMouseButtonDown(0))
        {
            MoveObjectToPosition(targetPosition);
            ExitObjectMovementMode();
        }
    }

    private Vector3 GetMouseWorldPosition()
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int environmentLayerMask = LayerMask.GetMask("EnvironmentLayer"); // Define the layer mask
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
        {
            return hit.point;
        }
        return Vector3.zero;
    }

    private void MoveObjectToPosition(Vector3 position)
    {
        if (selectedObject != null)
        {
            selectedObject.transform.position = position;
        }
    }

    private void ExitObjectMovementMode()
    {
        isMovingObject = false;
        selectedObject = null;
        relocationIndicator.SetActive(false); // Hide the placement indicator
        PlayerDominoPlacement.Instance.TogglePlacementControls(true);
    }
}