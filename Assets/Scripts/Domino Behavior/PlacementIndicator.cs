using UnityEngine;
using DG.Tweening;
using UnityEngine.Events;

[ExecuteInEditMode]
public class PlacementIndicator : DominoLike
{
    #region Fields and Properties
    private Renderer indicatorRenderer;
    public BoxCollider placementCollider; //Collider for placement detection
    public BoxCollider snapCollider; // Collider for snapping to the ground
    public Domino trackedDomino;
    public Rigidbody trackedDominoRb;
    private Material indicatorMaterial;
    private Material outlineMaterial;
    public string UniqueID => $"{transform.position.x:F2}_{transform.position.y:F2}_{transform.position.z:F2}";

    [Header("Indicator Settings")]
    static DominoSoundManager soundManager;
    public static readonly float fadeSpeed = 2f;
    static readonly float maxAlpha = .25f;
    static readonly float placementThreshold = 0.1f; // Distance threshold for placement
    static readonly float alignmentAngleThreshold = 10f; // Angle threshold for alignment
    public Color indicatorColor = Color.white; // Color of the indicator
    public bool colorHidden = false; // Flag to hide the color
    public DominoSoundManager.DominoSoundType soundType; // Sound type for the placement indicator

    public enum IndicatorState { Empty, TryingToFill, Filled, Disabled } // Define states
    public IndicatorState currentState = IndicatorState.Empty; // Current state
    public static UnityEvent<PlacementIndicator> OnIndicatorFilled = new();
    public static UnityEvent<PlacementIndicator> OnIndicatorEmptied = new();
    public UnityEvent OnIndicatorFilledInstance = new(); // Non-static event for individual indicators
    public UnityEvent OnIndicatorEmptiedInstance = new();

    #endregion

    #region Unity Methods

    void Awake()
    {
        indicatorRenderer = GetComponent<Renderer>();
        placementCollider = GetComponent<BoxCollider>();
        indicatorMaterial = new Material(indicatorRenderer.sharedMaterials[0]);
        outlineMaterial = new Material(indicatorRenderer.sharedMaterials[1]);
        indicatorRenderer.materials = new[] { indicatorMaterial, outlineMaterial }; // Assign instanced materials
    }
    void Start()
    {
        CheckAndResolveOverlap(); // Check for overlaps with other indicators
        SnapToGround();
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
        if (snapCollider != null) 
        {
            DestroyImmediate(snapCollider); // Disable the snap collider
            snapCollider = null; // Set to null to avoid further use
        }
    }

    void Update()
    {
        if (Application.isPlaying)
        {
            switch (currentState)
            {
                case IndicatorState.Empty:
                    // Wait for a collision with a domino
                    break;
                case IndicatorState.Disabled:
                    // Nothing can happen until enabled
                    break;

                case IndicatorState.TryingToFill:
                    if (trackedDomino != null)
                    {
                        // Debug.DrawLine(transform.position, trackedDomino.transform.position, Color.yellow);
                        CheckDominoPlacement();
                    }
                    break;

                case IndicatorState.Filled:
                    if (trackedDomino.currentState == Domino.DominoState.Held)
                    {
                        currentState = IndicatorState.TryingToFill;
                        OnIndicatorEmptied.Invoke(this); // Notify that the indicator was filled and is now empty
                        OnIndicatorEmptiedInstance.Invoke(); // Notify individual subscribers
                        Debug.Log("Indicator fading in because the domino was lifted: " + trackedDomino.name);
                        FadeIn(); // Fade back in if the domino is removed
                    }
                    break;
            }
        }
        else {
            if (colorHidden) indicatorMaterial.color = Color.white; // Set material to white in the editor
            else indicatorMaterial.color = indicatorColor; // Restore the internal color in the editor
            indicatorMaterial.color = new Color(indicatorMaterial.color.r, indicatorMaterial.color.g, indicatorMaterial.color.b, maxAlpha);
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DominoTag") && trackedDomino == null && currentState != IndicatorState.Filled && currentState != IndicatorState.Disabled)
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
        if (other.CompareTag("DominoTag") && trackedDomino == null && currentState != IndicatorState.Filled && currentState != IndicatorState.Disabled)
        {
            trackedDomino = other.gameObject.GetComponent<Domino>();
            trackedDominoRb = other.gameObject.GetComponent<Rigidbody>();
            currentState = IndicatorState.TryingToFill;
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (!other.CompareTag("DominoTag") || DominoResetManager.Instance != null && DominoResetManager.Instance.currentState == DominoResetManager.ResetState.Idle)
        {
            return; // Ignore if the collider is not a domino
        }
        if (other.gameObject == trackedDomino?.gameObject &&
        currentState != IndicatorState.Disabled &&
        trackedDomino?.currentState != Domino.DominoState.Animating &&
        Vector3.Distance(transform.position, trackedDomino.transform.position) > 0.001f
        )
        {
            if (currentState == IndicatorState.Filled)
            {
                OnIndicatorEmptied.Invoke(this); // Notify that the indicator was filled and is now empty;
                OnIndicatorEmptiedInstance.Invoke();
            }
            Debug.Log("Indicator fading in because the domino fell out, distance: " + Vector3.Distance(transform.position, trackedDomino.transform.position));
            trackedDomino.placementIndicator = null; // Clear the domino's reference to this indicator 
            // Reset the tracked domino and its Rigidbody
            trackedDomino = null;
            trackedDominoRb = null;
            currentState = IndicatorState.TryingToFill; // Transition to Empty state
            FadeIn(); // Fade back in if the domino is removed
        }
    }
    #endregion

    #region Placement Logic
    private void CheckDominoPlacement()
    {
        if (trackedDominoRb == null ||
            Vector2.Distance(new Vector2(trackedDomino.transform.position.x, trackedDomino.transform.position.z), 
            new Vector2(transform.position.x, transform.position.z)) > placementThreshold)
        {
            trackedDomino = null;
            trackedDominoRb = null;
            currentState = IndicatorState.Empty; // Transition to Empty state if too far away
            return;
        }
        if (trackedDomino.currentState == Domino.DominoState.Held ||
        trackedDomino.currentState == Domino.DominoState.Animating ||
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
            currentState = IndicatorState.Filled;
            PlaceDomino();
        }
    }

    public void RestoreProgress()
    {
        trackedDomino = Instantiate(GameManager.Instance.dominoPrefab, transform.position, transform.rotation).GetComponent<Domino>();
        trackedDominoRb = trackedDomino.GetComponent<Rigidbody>();
        trackedDomino.GetComponent<DominoSkin>().colorOverride = indicatorColor;
        Domino.OnDominoPlacedCorrectly.Invoke(trackedDomino);
        FadeOut(false); // Fade out the indicator and outline
        currentState = IndicatorState.Filled;
        OnIndicatorFilled.Invoke(this); // Notify that the indicator is filled (static event)
        OnIndicatorFilledInstance.Invoke(); // Notify individual subscribers
        trackedDomino.placementIndicator = this;
        placementCollider.enabled = false;
        if (soundType != DominoSoundManager.DominoSoundType.Default) trackedDomino.soundType = soundType;
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
        if (soundType != DominoSoundManager.DominoSoundType.Default) trackedDomino.soundType = soundType;
        // Reset the domino's position using the rotate reset animation
        trackedDominoRb.GetComponent<DominoSkin>().colorOverride = indicatorColor; // Use the internal color for the domino
        trackedDomino.AnimateDomino(Domino.DominoAnimation.Rotate);

        FadeOut(); // Fade out the indicator and outline
        currentState = IndicatorState.Filled;
        OnIndicatorFilled.Invoke(this); // Notify that the indicator is filled (static event)
        OnIndicatorFilledInstance.Invoke(); // Notify individual subscribers
        trackedDomino.placementIndicator = this;
        GameManager.Instance.CheckCompletion(); // Check if all indicators are filled
        Domino.OnDominoPlacedCorrectly.Invoke(trackedDomino);
        placementCollider.enabled = false;
        // Debug.Log($"Indicator {GetInstanceID()} filled."); // Log the unique ID for debugging
    }
    #endregion

    #region Visual Effects
    public void FadeOut(bool playSound = true)
    {
        if (playSound) soundManager.PlayPlacementSound(1);
        FadeOutline(0f, fadeSpeed);
        indicatorMaterial.DOKill();
        placementCollider.enabled = false;
        indicatorMaterial.DOFade(0f, fadeSpeed).OnComplete(() =>
        {
            indicatorRenderer.enabled = false; // Disable the renderer after fading out
            placementCollider.enabled = true;
        });
    }

    public void FadeIn(bool playSound = true)
    {
        if (playSound) soundManager.PlayPlacementSound(-1);
        FadeOutline(.5f, fadeSpeed);
        placementCollider.enabled = true;
        indicatorRenderer = GetComponent<Renderer>();
        indicatorRenderer.enabled = true;
        indicatorMaterial.DOKill();
        indicatorMaterial.DOFade(maxAlpha, fadeSpeed);
    }

    public void FadeOutline(float targetAlpha, float duration)
    {
        outlineMaterial.DOKill();
        outlineMaterial.DOFade(targetAlpha, duration).OnComplete(() =>
        {
            if (targetAlpha == 1) outlineMaterial.DOFade(0.5f, duration);
        });
    }

    public void ApplyColor(Color inputColor)
    {
        indicatorColor = inputColor;
        inputColor.a = Mathf.Clamp(inputColor.a, 0f, maxAlpha);
        indicatorMaterial.color = inputColor;
        Color outlineColor = Color.white;
        outlineColor.a = Mathf.Clamp(outlineColor.a, 0f, maxAlpha);
        outlineMaterial.color = outlineColor;
    }
    #endregion
}