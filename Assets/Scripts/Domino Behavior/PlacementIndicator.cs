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
    [SerializeField] private Domino trackedDomino;
    private Rigidbody trackedDominoRb;

    [Header("Indicator Settings")]
    static DominoSoundManager soundManager;
    public static readonly float fadeSpeed = 2f;
    static readonly float maxAlpha = .25f;
    static readonly float placementThreshold = 0.1f; // Distance threshold for placement
    static readonly float alignmentAngleThreshold = 10f; // Angle threshold for alignment
    public Color indicatorColor = Color.white; // Color of the indicator
    public DominoSoundManager.DominoSoundType soundType; // Sound type for the placement indicator

    public enum IndicatorState { Empty, TryingToFill, Filled, Disabled } // Define states
    public IndicatorState currentState = IndicatorState.Empty; // Current state
    public static UnityEvent<PlacementIndicator> OnIndicatorFilled = new();
    public static UnityEvent<PlacementIndicator> OnIndicatorEmptied = new();
    public UnityEvent OnIndicatorFilledInstance = new(); // Non-static event for individual indicators
    public UnityEvent OnIndicatorEmptiedInstance = new();

    [Header("Line Renderer Settings")]
    [SerializeField] private LineRenderer[] edgeLineRenderers; // Line renderers for edges
    [SerializeField] private LineRenderer[] sideLineRenderers; // Line renderers for sides
    #endregion

    #region Unity Methods
    void Start()
    {
        indicatorRenderer = GetComponent<Renderer>();
        placementCollider = GetComponent<BoxCollider>();
        CheckAndResolveOverlap(); // Check for overlaps with other indicators
        SnapToGround();
        if (soundManager == null) soundManager = FindObjectOfType<DominoSoundManager>(); // Get references
        if (snapCollider != null) 
        {
            DestroyImmediate(snapCollider); // Disable the snap collider
            snapCollider = null; // Set to null to avoid further use
        }
        // InitializeLineRenderers();
    }

    void Update()
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
                    Debug.DrawLine(transform.position, trackedDomino.transform.position, Color.yellow);
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
        if (!other.CompareTag("DominoTag"))
        {
            return; // Ignore if the collider is not a domino
        }
        if (other.gameObject == trackedDomino?.gameObject && currentState != IndicatorState.Disabled && trackedDomino?.currentState != Domino.DominoState.Animating)
        {
            if (currentState == IndicatorState.Filled)
            {
                OnIndicatorEmptied.Invoke(this); // Notify that the indicator was filled and is now empty;
                OnIndicatorEmptiedInstance.Invoke();
            }
            trackedDomino.placementIndicator = null; // Clear the domino's reference to this indicator 
            // Reset the tracked domino and its Rigidbody
            trackedDomino = null;
            trackedDominoRb = null;
            currentState = IndicatorState.TryingToFill; // Transition to Empty state
            Debug.Log("Indicator fading in because the domino fell out");
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
        // Assign the sound type to the domino
        if (soundType != DominoSoundManager.DominoSoundType.Default) trackedDomino.soundType = soundType;
        // Reset the domino's position using the rotate reset animation
        trackedDominoRb.GetComponent<DominoSkin>().TweenColor(indicatorColor, 1f); // Tween the color of the domino
        trackedDomino.AnimateDomino(Domino.DominoAnimation.Rotate);

        // Fade out the indicator
        FadeOut();
        // Debug.Log("Indicator filled: " + trackedDomino.name);
        currentState = IndicatorState.Filled; // Transition to Placed state
        OnIndicatorFilled.Invoke(this); // Notify that the indicator is filled (static event)
        OnIndicatorFilledInstance.Invoke(); // Notify individual subscribers
        trackedDomino.placementIndicator = this;
        GameManager.Instance.CheckCompletion(); // Check if all indicators are filled
        Domino.OnDominoPlacedCorrectly.Invoke(trackedDomino);
        placementCollider.enabled = false; // Disable the placement collider
    }
    #endregion

    #region Visual Effects
    public void FadeOut(bool playSound = true)
    {
        if (playSound) soundManager.PlayPlacementSound(1);
        // Debug.Log("Indicator fading out: " + gameObject.name);

        indicatorRenderer.material.DOKill();
        placementCollider.enabled = false;
        // Use DOTween to fade out the material's alpha
        indicatorRenderer.material.DOFade(0f, fadeSpeed).OnComplete(() =>
        {
            indicatorRenderer.enabled = false; // Disable the renderer after fading out
            placementCollider.enabled = true;
        });

        foreach (var lineRenderer in edgeLineRenderers) lineRenderer.enabled = false;
        foreach (var lineRenderer in sideLineRenderers) lineRenderer.enabled = false;
    }

    public void FadeIn(bool playSound = true)
    {
        if (playSound) soundManager.PlayPlacementSound(-1);
        // Debug.Log("Indicator fading in: " + gameObject.name);
        placementCollider.enabled = true;
        indicatorRenderer.enabled = true;
        indicatorRenderer.material.DOKill();
        // Use DOTween to fade in the material's alpha
        indicatorRenderer.material.DOFade(maxAlpha, fadeSpeed);

        foreach (var lineRenderer in edgeLineRenderers) lineRenderer.enabled = true;
        foreach (var lineRenderer in sideLineRenderers) lineRenderer.enabled = true;
    }

    public void ApplyColor(Color inputColor)
    {
        indicatorColor = inputColor;
        inputColor.a = Mathf.Clamp(inputColor.a, 0f, maxAlpha);
        Renderer renderer = GetComponent<Renderer>();
        // Create a new material instance so we don't modify shared materials
        Material newMaterial = new(renderer.sharedMaterial);
        newMaterial.color = inputColor; // Assign instance to avoid modifying sharedMaterial
        renderer.material = newMaterial; // Assign the new material to the renderer
    }

    private void InitializeLineRenderers()
    {
        // Create line renderers for bottom edges
        edgeLineRenderers = new LineRenderer[4];
        Vector3[] corners = {
            new Vector3(-DominoLike.standardDimensions.x / 2, 0, -DominoLike.standardDimensions.z / 2) + transform.position + bottomPoint,
            new Vector3(DominoLike.standardDimensions.x / 2, 0, -DominoLike.standardDimensions.z / 2)+ transform.position + bottomPoint,
            new Vector3(DominoLike.standardDimensions.x / 2, 0, DominoLike.standardDimensions.z / 2)+ transform.position + bottomPoint,
            new Vector3(-DominoLike.standardDimensions.x / 2, 0, DominoLike.standardDimensions.z / 2)+ transform.position + bottomPoint
        };

        for (int i = 0; i < 4; i++)
        {
            edgeLineRenderers[i] = CreateLineRenderer();
            edgeLineRenderers[i].SetPositions(new[] { corners[i], corners[(i + 1) % 4] });
        }

        // Create line renderers for side edges
        sideLineRenderers = new LineRenderer[4];
        for (int i = 0; i < 4; i++)
        {
            sideLineRenderers[i] = CreateLineRenderer();
            Vector3 start = corners[i];
            Vector3 end = corners[i] + Vector3.up * DominoLike.standardDimensions.y;
            sideLineRenderers[i].SetPositions(new[] { start, end });

            // Apply gradient to fade out at the top
            Gradient gradient = new Gradient();
            gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) }
            );
            sideLineRenderers[i].colorGradient = gradient;
        }
    }

    private LineRenderer CreateLineRenderer()
    {
        GameObject lineObject = new GameObject("LineRenderer");
        lineObject.transform.SetParent(transform);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.useWorldSpace = false;
        return lineRenderer;
    }
    #endregion
}