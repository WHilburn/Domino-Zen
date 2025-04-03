using UnityEngine;
using UnityEngine.Rendering.Universal;

public class PlayerDominoPlacement : MonoBehaviour
{
    public static PlayerDominoPlacement Instance { get; private set; }
    public GameObject dominoPrefab;
    public GameObject handPrefab;
    public static GameObject heldDomino;
    private Rigidbody heldRb;
    private Transform heldHand;
    private Vector3 anchor;
    private DecalProjector decalProjector;
    private float savedDrag;
    private float savedAngularDrag;
    public float followSpeed = 1f;
    public float maxHandSpeed = 3f;
    public float hoverOffset = 1.6f;
    public float rotationSpeed = 100f;
    public Camera activeCamera;
    public GameObject lockSpritePrefab; // Reference to the lock sprite prefab

    private Vector3 handMouseOffset; // Offset between hand and mouse cursor

    void Start()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        activeCamera = FindFirstObjectByType<Camera>();
    }

    void Update()
    {
        if (heldDomino)
        {
            MoveHeldDomino();
            HandleRotation();
        }

        if (Input.GetKeyDown(KeyCode.Space))
        {
            if (heldDomino == null)
                SpawnDomino();
            else
                ReleaseDomino();
        }

        if (Input.GetMouseButtonDown(0)) // Left Click
        {
            if (heldDomino == null)
                TryPickUpDomino();
        }

        if (Input.GetKeyDown(KeyCode.Escape))
        {
            DeleteHeldDomino();
        }
    }

    void SpawnDomino()
    {
        if (!IsCameraActive()) return;

        Vector3 spawnPos = GetMouseWorldPosition();
        Quaternion spawnRotation = Quaternion.Euler(90f, 0f, 0f);

        heldDomino = Instantiate(dominoPrefab, spawnPos, spawnRotation);
        InitializeHeldDomino();

        CreateHand(spawnPos);
        AttachDominoToHand();

        // Calculate the offset between the hand and the mouse cursor
        handMouseOffset = heldHand.position - GetMouseWorldPosition();
    }

    void TryPickUpDomino()
    {
        if (!IsCameraActive()) return;

        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            Domino domino = hit.collider.GetComponent<Domino>();
            if (domino != null && !domino.isHeld)
            {
                if (domino.locked)
                {
                    Debug.Log("This domino is locked and cannot be picked up.");
                    domino.AnimateDomino(Domino.DominoAnimation.Jiggle);
                    ShowLockSprite(hit.point); // Show the lock sprite at the hit point
                    return;
                }
                PickUpDomino(domino.gameObject);
            }
                
        }
    }

    void PickUpDomino(GameObject domino)
    {
        heldDomino = domino;
        InitializeHeldDomino();

        if (IsDominoFalling())
        {
            ClearHeldDomino();
            return;
        }

        Vector3 spawnPos = AdjustSpawnPosition(heldDomino.transform.position);
        CreateHand(spawnPos);
        AttachDominoToHand();

        // Calculate the offset between the hand and the mouse cursor
        handMouseOffset = heldHand.position - GetMouseWorldPosition();
    }

    public void ReleaseDomino()
    {
        if (heldDomino == null) return;

        ResetDominoProperties();
        DestroyHand();

        ClearHeldDomino();
    }

    public void DeleteHeldDomino()
    {
        if (heldDomino == null) return;

        Destroy(heldDomino);
        DestroyHand();

        ClearHeldDomino();
    }

    void MoveHeldDomino()
    {
        if (heldHand == null) return;

        // Get the target position adjusted by the offset
        Vector3 targetPosition = GetMouseWorldPosition() + handMouseOffset;
        Vector3 targetFlat = new Vector3(targetPosition.x, heldHand.position.y, targetPosition.z);
        float step = Mathf.Min(maxHandSpeed * Time.deltaTime, Vector3.Distance(heldHand.position, targetFlat));
        heldHand.position = Vector3.MoveTowards(heldHand.position, targetFlat, step);
    }

    void HandleRotation()
    {
        if (Input.GetKey(KeyCode.Q))
            heldDomino.transform.Rotate(Vector3.forward, -rotationSpeed * Time.deltaTime);

        if (Input.GetKey(KeyCode.E))
            heldDomino.transform.Rotate(Vector3.forward, rotationSpeed * Time.deltaTime);
    }

    Vector3 GetMouseWorldPosition()
    {
        Ray ray = activeCamera.ScreenPointToRay(Input.mousePosition);
        int environmentLayerMask = LayerMask.GetMask("EnvironmentLayer");
        if (Physics.Raycast(ray, out RaycastHit hit, Mathf.Infinity, environmentLayerMask))
            return hit.point + Vector3.up * hoverOffset;
        return ray.origin + ray.direction * 5f;
    }

    // Helper Methods
    private bool IsCameraActive()
    {
        return activeCamera != null && activeCamera.enabled;
    }

    private void InitializeHeldDomino()
    {
        heldDomino.layer = LayerMask.NameToLayer("Ignore Raycast");
        heldDomino.GetComponent<Domino>().isHeld = true;
        decalProjector = heldDomino.GetComponent<DecalProjector>();
        if (decalProjector) decalProjector.enabled = true;

        heldRb = heldDomino.GetComponent<Rigidbody>();
        savedDrag = heldRb.drag;
        savedAngularDrag = heldRb.angularDrag;
        heldRb.drag = 10f;
        heldRb.angularDrag = 90f;
        heldRb.constraints = RigidbodyConstraints.FreezeRotationZ;
    }

    private bool IsDominoFalling()
    {
        return heldRb.velocity.magnitude > 1f || heldRb.angularVelocity.magnitude > 1f;
    }

    private void ClearHeldDomino()
    {
        heldDomino = null;
        heldRb = null;
        heldHand = null;
    }

    private Vector3 AdjustSpawnPosition(Vector3 position)
    {
        RaycastHit hit;
        if (Physics.Raycast(position, Vector3.down, out hit, 3f))
            position = hit.point;
        else
            position.y -= heldDomino.transform.localScale.y * 0.5f;

        position.y += hoverOffset;
        return position;
    }

    private void CreateHand(Vector3 spawnPos)
    {
        GameObject handObject = Instantiate(handPrefab, spawnPos, Quaternion.identity);
        heldHand = handObject.transform;
        heldHand.position = spawnPos + new Vector3(0f, 0.5f, 0f);
    }

    private void AttachDominoToHand()
    {
        heldDomino.transform.position = heldHand.position;

        SpringJoint spring = heldDomino.AddComponent<SpringJoint>();
        spring.connectedBody = heldHand.GetComponent<Rigidbody>();
        spring.anchor = heldDomino.GetComponent<Domino>().holdPoint;
        anchor = spring.anchor;
        spring.autoConfigureConnectedAnchor = false;
        spring.connectedAnchor = Vector3.zero;
        spring.tolerance = 0.001f;
        spring.spring = 500f;
        spring.damper = 1f;
        spring.massScale = 1f;

        heldRb.velocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
    }

    private void ResetDominoProperties()
    {
        heldDomino.layer = LayerMask.NameToLayer("Default");
        heldDomino.GetComponent<Domino>().isHeld = false;

        SpringJoint spring = heldDomino.GetComponent<SpringJoint>();
        if (spring != null) Destroy(spring);

        heldRb.useGravity = true;
        if (decalProjector) decalProjector.enabled = false;
        heldRb.velocity = Vector3.zero;
        heldRb.angularVelocity = Vector3.zero;
        heldRb.drag = savedDrag;
        heldRb.angularDrag = savedAngularDrag;
        heldRb.constraints = RigidbodyConstraints.None;
    }

    private void DestroyHand()
    {
        if (heldHand != null)
            Destroy(heldHand.gameObject);
    }

    void ShowLockSprite(Vector3 position)
    {
        if (lockSpritePrefab == null) return;

        GameObject lockSprite = Instantiate(lockSpritePrefab, position + Vector3.up * 0.5f, Quaternion.identity);
        SpriteRenderer spriteRenderer = lockSprite.GetComponent<SpriteRenderer>();
        if (spriteRenderer != null)
        {
            StartCoroutine(FadeOutAndDestroy(spriteRenderer, 1f)); // Fade out over 1 second
        }
    }

    System.Collections.IEnumerator FadeOutAndDestroy(SpriteRenderer spriteRenderer, float duration)
    {
        float elapsed = 0f;
        Color originalColor = spriteRenderer.color;

        while (elapsed < duration)
        {
            elapsed += Time.deltaTime;
            float alpha = Mathf.Lerp(1f, 0f, elapsed / duration);
            spriteRenderer.color = new Color(originalColor.r, originalColor.g, originalColor.b, alpha);
            yield return null;
        }

        Destroy(spriteRenderer.gameObject);
    }
}