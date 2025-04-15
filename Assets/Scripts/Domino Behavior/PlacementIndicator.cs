using UnityEngine;
using DG.Tweening;
using UnityEngine.UIElements;
using UnityEngine.Events;

[ExecuteInEditMode]
public class PlacementIndicator : DominoLike
{
    #region Fields and Properties
    private Renderer indicatorRenderer;
    public BoxCollider placementCollider; //Collider for placement detection
    public BoxCollider snapCollider; // Collider for snapping to the ground
    private Domino trackedDomino;
    private Rigidbody trackedDominoRb;

    [Header("Indicator Settings")]
    static DominoSoundManager soundManager;
    static readonly float fadeSpeed = 2f;
    static readonly float maxAlpha = 1f;
    static readonly float placementThreshold = 0.2f; // Distance threshold for placement
    static readonly float alignmentAngleThreshold = 10f; // Angle threshold for alignment
    public Color indicatorColor = Color.white; // Color of the indicator

    public enum IndicatorState { Empty, TryingToFill, Filled } // Define states
    public IndicatorState currentState = IndicatorState.Empty; // Current state
    public static UnityEvent<PlacementIndicator> OnIndicatorFilled = new();
    public static UnityEvent<PlacementIndicator> OnIndicatorEmptied = new();
    #endregion

    #region Unity Methods
    void Start()
    {
        indicatorRenderer = GetComponent<Renderer>();
        placementCollider = GetComponent<BoxCollider>();
        CheckAndResolveOverlap(); // Check for overlaps with other indicators
        SnapToGround();
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
    }

    void Update()
    {
        switch (currentState)
        {
            case IndicatorState.Empty:
                // Wait for a collision with a domino
                break;

            case IndicatorState.TryingToFill:
                if (trackedDomino != null)
                {
                    Debug.DrawLine(transform.position, trackedDomino.transform.position, Color.yellow);
                    CheckDominoPlacement();
                }
                break;

            case IndicatorState.Filled:
                if (trackedDomino.currentState == Domino.DominoState.Held)
                {
                    currentState = IndicatorState.Empty; // Transition to Empty state
                    OnIndicatorEmptied.Invoke(this); // Notify that the indicator was filled and is now empty
                    FadeIn(); // Fade back in if the domino is removed
                }
                break;
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DominoTag") && trackedDomino == null && currentState != IndicatorState.Filled)
        {
            trackedDomino = other.gameObject.GetComponent<Domino>();
            if (trackedDomino.placementIndicator != null && trackedDomino.placementIndicator != this) //If the domino is also paired to another indicator, ignore it
            {
                trackedDomino = null; // Clear the tracked domino if it's already paired with another indicator
                return;
            }
            trackedDominoRb = other.gameObject.GetComponent<Rigidbody>();
            currentState = IndicatorState.TryingToFill;
        }
    }

    private void OnTriggerStay(Collider other)
    {
        if (other.CompareTag("DominoTag") && trackedDomino == null && currentState != IndicatorState.Filled)
        {
            trackedDomino = other.gameObject.GetComponent<Domino>();
            trackedDominoRb = other.gameObject.GetComponent<Rigidbody>();
            currentState = IndicatorState.TryingToFill;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == trackedDomino?.gameObject)
        {
            if (currentState == IndicatorState.Filled) OnIndicatorEmptied.Invoke(this); // Notify that the indicator was filled and is now empty;
            trackedDomino.placementIndicator = null; // Clear the domino's reference to this indicator 
            // Reset the tracked domino and its Rigidbody
            trackedDomino = null;
            trackedDominoRb = null;
            currentState = IndicatorState.Empty; // Transition to Empty state
            FadeIn(); // Fade back in if the domino is removed
        }
    }
    #endregion

    #region Placement Logic
    private void CheckDominoPlacement()
    {
        if (trackedDominoRb == null ||
            Vector3.Distance(trackedDomino.transform.position, transform.position) > 1)
        {
            trackedDomino = null;
            currentState = IndicatorState.Empty; // Transition to Empty state if too far away
            return;
        }
        if (trackedDomino.currentState == Domino.DominoState.Held ||
        trackedDomino.currentState == Domino.DominoState.Animating ||
            // trackedDominoRb.velocity.magnitude > 0.05f ||
            trackedDominoRb.angularVelocity.magnitude > 0.05f)
        {
            return;
        }

        // Check alignment and position, accepts the domino if it's facing backwards or forwards
        bool isAligned = (Vector3.Angle(trackedDomino.transform.forward, transform.forward) < alignmentAngleThreshold ||
                          Vector3.Angle(trackedDomino.transform.forward, -transform.forward) < alignmentAngleThreshold) &&
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
        if (trackedDomino.stablePositionSet || //Dont allow domino to be placed if it already has a stable position set
        DominoResetManager.Instance != null && 
        DominoResetManager.Instance.currentState != DominoResetManager.ResetState.Idle && // Prevent placement if there are fallen dominoes and we're waiting for a reset or in the middle of resetting
        !DominoResetManager.Instance.checkpointedDominoes.Contains(trackedDomino))// Prevent placing a second time if the domino is checkpointed
        {
            return; 
        }

        // Set the domino's stable position and rotation
        trackedDomino.SaveStablePosition(transform);
        // Reset the domino's position using the rotate reset animation
        trackedDominoRb.GetComponent<DominoSkin>().TweenColor(indicatorColor, 1f); // Tween the color of the domino
        trackedDomino.AnimateDomino(Domino.DominoAnimation.Rotate);

        // Fade out the indicator
        FadeOut();
        Debug.Log("Indicator filled: " + trackedDomino.name);
        currentState = IndicatorState.Filled; // Transition to Placed state
        OnIndicatorFilled.Invoke(this); // Notify that the indicator is filled
        trackedDomino.placementIndicator = this;
        GameManager.Instance.CheckCompletion(); // Check if all indicators are filled
        Domino.OnDominoPlacedCorrectly.Invoke(trackedDomino);
    }
    #endregion

    #region Visual Effects
    public void FadeOut(bool playSound = true)
    {
        if (playSound) soundManager.PlayPlacementSound(1);

        indicatorRenderer.material.DOKill();
        // Use DOTween to fade out the material's alpha
        indicatorRenderer.material.DOFade(0f, fadeSpeed).OnComplete(() =>
        {
            indicatorRenderer.enabled = false; // Disable the renderer after fading out
        });
    }

    public void FadeIn(bool playSound = true)
    {
        if (playSound) soundManager.PlayPlacementSound(-1);
        indicatorRenderer.enabled = true;
        indicatorRenderer.material.DOKill();
        // Use DOTween to fade in the material's alpha
        indicatorRenderer.material.DOFade(maxAlpha, fadeSpeed);
    }

    public void ApplyColor(Color inputColor)
    {
        indicatorColor = inputColor;
        Renderer renderer = GetComponent<Renderer>();
        // Create a new material instance so we don't modify shared materials
        Material newMaterial = new(renderer.sharedMaterial);
        newMaterial.color = inputColor; // Assign instance to avoid modifying sharedMaterial
        renderer.material = newMaterial; // Assign the new material to the renderer
    }
    #endregion
}