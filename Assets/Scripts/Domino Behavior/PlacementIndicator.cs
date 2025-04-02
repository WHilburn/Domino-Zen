using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;

[ExecuteInEditMode]
public class PlacementIndicator : MonoBehaviour
{
    private Renderer indicatorRenderer;
    public BoxCollider placementCollider; //Collider for placement detection
    public BoxCollider snapCollider; // Collider for snapping to the ground
    private Domino trackedDomino;
    private Rigidbody trackedDominoRb;

    [Header("Indicator Settings")]
    static DominoSoundManager soundManager;
    static float fadeSpeed = 2f;
    static float maxAlpha = 1f;
    static float placementThreshold = 0.2f; // Distance threshold for placement
    static float alignmentAngleThreshold = 10f; // Angle threshold for alignment
    public Color indicatorColor = Color.white; // Color of the indicator

    public enum IndicatorState { Empty, Occupied, Placed } // Define states
    public IndicatorState currentState = IndicatorState.Empty; // Current state

    void Start()
    {
        indicatorRenderer = GetComponent<Renderer>();
        placementCollider = GetComponent<BoxCollider>();
        SnapToGround();
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
    }

    private void OnTriggerEnter(Collider other)
    {
        Debug.Log("Enter Triggered");
        if (other.CompareTag("DominoTag") && trackedDomino == null)
        {
            trackedDomino = other.gameObject.GetComponent<Domino>();
            trackedDominoRb = other.gameObject.GetComponent<Rigidbody>();
            currentState = IndicatorState.Occupied; // Transition to Occupied state
        }
    }

    private void OnTriggerExit(Collider other)
    {
        Debug.Log("Exit Triggered");
        if (other.gameObject == trackedDomino?.gameObject)
        {
            trackedDomino = null;
            trackedDominoRb = null;
            currentState = IndicatorState.Empty; // Transition to Empty state
            FadeIn(); // Fade back in if the domino is removed
        }
    }

    void Update()
    {
        switch (currentState)
        {
            case IndicatorState.Empty:
                // Wait for a collision with a domino
                break;

            case IndicatorState.Occupied:
                if (trackedDomino != null)
                {
                    CheckDominoPlacement();
                }
                break;

            case IndicatorState.Placed:
                if (trackedDomino.isHeld)
                {
                    currentState = IndicatorState.Occupied; // Transition to Empty state
                    FadeIn(); // Fade back in if the domino is removed
                }
                break;
        }
    }

    private void CheckDominoPlacement()
    {
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
        currentState = IndicatorState.Placed; // Transition to Placed state
        GameManager.Instance.CheckCompletion(); // Check if all indicators are filled
    }

    private void FadeOut()
    {
        Debug.Log("FadeOut called");
        soundManager.PlayPlacementSound(1);

        indicatorRenderer.material.DOKill();
        // Use DOTween to fade out the material's alpha
        indicatorRenderer.material.DOFade(0f, fadeSpeed).OnComplete(() =>
        {
            indicatorRenderer.enabled = false; // Disable the renderer after fading out
        });
    }

    private void FadeIn()
    {
        Debug.Log("FadeIn called");
        soundManager.PlayPlacementSound(-1);
        indicatorRenderer.enabled = true;
        indicatorRenderer.material.DOKill();
        // Use DOTween to fade in the material's alpha
        indicatorRenderer.material.DOFade(maxAlpha, fadeSpeed);
    }

    private void SnapToGround()
    {
        if (snapCollider.enabled == false) return;
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity))
        {
            float bottomOffset = snapCollider.bounds.extents.y; // Get half the height
            transform.position = hitInfo.point + Vector3.up * bottomOffset;
        }
        //Disable the collider if it is not needed anymore
        snapCollider.enabled = false; // Disable the collider
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