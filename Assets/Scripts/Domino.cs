using UnityEngine;
using System.Collections;

public class Domino : MonoBehaviour
{
    private Rigidbody rb;
    private float stillnessThreshold = 5f;  // Velocity threshold to consider "stationary"
    public Vector3 holdPoint; // Offset from center to hold the domino
    public DominoSoundManager soundManager;
    public CameraController cameraController;
    public bool isMoving = false;
    public bool isHeld = false;
    public float velocityMagnitude;
    public DominoMaterialList materialList; // Reference to the shared material list

    void Start()
    {
        rb = GetComponent<Rigidbody>();
        rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic; // Start with high accuracy
        soundManager = FindObjectOfType<DominoSoundManager>(); // Get reference
        cameraController = FindObjectOfType<CameraController>();

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
            renderer.material = chosenMaterial;
        }

    }

    void OnDestroy()
    {
        cameraController?.fallingDominoes.Remove(transform);
    }

    void Update()
    {
        if (isHeld)
        {
            return;
        }
        
        if (rb != null && soundManager != null)
        {
            velocityMagnitude = rb.angularVelocity.magnitude;
            soundManager.UpdateDominoMovement(velocityMagnitude);
        }

        bool currentlyMoving = rb.velocity.sqrMagnitude >= stillnessThreshold * stillnessThreshold || 
        rb.angularVelocity.sqrMagnitude >= stillnessThreshold * stillnessThreshold;

        if (currentlyMoving && !isMoving)
        {
            isMoving = true;
            if (!cameraController.fallingDominoes.Contains(transform))
            {
                cameraController.fallingDominoes.Add(transform);
            }
            StartCoroutine(RemoveFromFallingDominoes(0.25f));
            rb.collisionDetectionMode = CollisionDetectionMode.ContinuousDynamic;

        }
        else if (!currentlyMoving && isMoving)
        {
            isMoving = false;
            rb.collisionDetectionMode = CollisionDetectionMode.Discrete;
        }
        isMoving = currentlyMoving;
    }

    public IEnumerator RemoveFromFallingDominoes(float delay)
    {
        yield return new WaitForSeconds(delay);
        cameraController?.fallingDominoes.Remove(transform);
    }

}
