using UnityEngine;
using DG.Tweening;

[ExecuteInEditMode]
public class PlacementIndicator : MonoBehaviour
{
    private Renderer indicatorRenderer;
    private Collider placementCollider;
    private Domino trackedDomino;
    public Rigidbody trackedDominoRb;
    public bool isFadingOut = false;

    [Header("Indicator Settings")]
    static DominoSoundManager soundManager;
    static float fadeSpeed = 2f;
    static float maxAlpha = 0.6f;
    static float placementThreshold = 0.2f; // Distance threshold for placement
    static float alignmentAngleThreshold = 10f; // Angle threshold for alignment
    public Color indicatorColor = Color.blue; // Color of the indicator

    void Start()
    {
        indicatorRenderer = GetComponent<Renderer>();
        placementCollider = GetComponent<Collider>();
        SnapToGround();
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DominoTag") && trackedDomino == null)
        {
            trackedDomino = other.gameObject.GetComponent<Domino>();
            trackedDominoRb = other.gameObject.GetComponent<Rigidbody>();
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == trackedDomino)
        {
            trackedDomino = null;
            trackedDominoRb = null;
            FadeIn(); // Fade back in if the domino is removed
        }
    }

    void Update()
    {
        if (trackedDomino != null && !isFadingOut)
        {
            CheckDominoPlacement();
        }
    }

    private void CheckDominoPlacement()
    {
        // Ensure the domino is stationary
        if (trackedDominoRb == null ||
        trackedDomino.isHeld ||
        trackedDominoRb.velocity.magnitude > 0.05f ||
         trackedDominoRb.angularVelocity.magnitude > 0.05f)
        {
            return;
        }

        // Check alignment and position
        bool isAligned = Vector3.Angle(trackedDomino.transform.forward, transform.forward) < alignmentAngleThreshold &&
                         Vector3.Angle(trackedDomino.transform.up, transform.up) < alignmentAngleThreshold;

        bool isPositioned = Vector3.Distance(new Vector3(trackedDomino.transform.position.x, 0, trackedDomino.transform.position.z),
                                             new Vector3(transform.position.x, 0, transform.position.z)) < placementThreshold;

        if (isAligned && isPositioned)
        {
            PlaceDomino();
        }
    }

    private void PlaceDomino()
    {
        // Set the domino's stable position and rotation
        trackedDomino.GetComponent<Domino>().SetStablePosition(transform);

        // Reset the domino's position using the rotate reset animation
        trackedDomino.GetComponent<Domino>().ResetDomino(Domino.ResetAnimation.Rotate);

        trackedDominoRb.GetComponent<DominoSkin>().TweenColor(indicatorColor, 1f); // Tween the color of the domino

        // Fade out the indicator
        FadeOut();
    }

    private void FadeOut()
    {
        isFadingOut = true;
        soundManager.PlayPlacementSound(1);

        // Use DOTween to fade out the material's alpha
        indicatorRenderer.material.DOFade(0f, fadeSpeed).OnComplete(() =>
        {
            indicatorRenderer.enabled = false; // Disable the renderer after fading out
        });
    }

    private void FadeIn()
    {
        soundManager.PlayPlacementSound(-1);
        isFadingOut = false;
        indicatorRenderer.enabled = true;

        // Use DOTween to fade in the material's alpha
        indicatorRenderer.material.DOFade(maxAlpha, fadeSpeed);
    }

    private void SnapToGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity))
        {
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                float bottomOffset = box.bounds.extents.y; // Get half the height
                transform.position = hitInfo.point + Vector3.up * bottomOffset;
            }
        }
    }
    public void ApplyColor(Color inputColor)
    {
        indicatorColor = inputColor;
        Renderer renderer = GetComponent<Renderer>();
        // Create a new material instance so we don't modify shared materials
        Material newMaterial = new Material(renderer.sharedMaterial);
        newMaterial.color = inputColor; // Assign instance to avoid modifying sharedMaterial
        renderer.material = newMaterial; // Assign the new material to the renderer
    }
}