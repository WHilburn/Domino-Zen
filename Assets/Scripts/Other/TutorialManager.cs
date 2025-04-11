using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;
using Cinemachine;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    private bool isTutorialActive = false;
    private int currentStepIndex = 0;
    public List<TutorialStep> steps;
    public Button tutorialButton;
    public TextMeshProUGUI tutorialText; // Reference to the UI Text element
    public GameObject arrowSpritePrefab; // Instance of the arrow sprite
    private float bobbingSpeed = 3f; // Speed of the bobbing animation
    private float bobbingHeight = 10f; // Height of the bobbing animation
    private bool placementEnabled = false; // Flag to enable/disable controls
    public static UnityEvent<bool> OnTogglePlacementControls = new(); //Events to enable/disable controls
    public static UnityEvent<bool> OnToggleCameraControls = new();
    public Camera mainCamera; // Reference to the main camera
    public CinemachineVirtualCamera starterCamera; // Reference to the starter camera
    public Canvas uiCanvas; // Reference to the UI Canvas
    private Transform currentTarget; // Store the current target for the arrow
    private bool visible = true; // Flag to show/hide the tutorial

    public TutorialIndicatorCheck tutorialIndicatorCheck1;
    public TutorialIndicatorCheck tutorialIndicatorCheck2; // Reference to the tutorial indicator checks

    private List<GameObject> activeArrows = new(); // List to track active arrow instances
    private Dictionary<GameObject, Transform> arrowTargets = new(); // Map arrows to their target transforms

    private AudioSource audioSource; // Reference to the audio source for playing sounds

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        audioSource = GetComponent<AudioSource>(); // Get the AudioSource component
    }

    void Start()
    {
        if (steps.Count > 0)
        {
            StartTutorial();
        }

        SubscribeToEvents();
    }

    private void SubscribeToEvents()
    {
        tutorialIndicatorCheck1.OnCompleteIndicatorRow.AddListener(OnCompleteIndicatorRow1);
        tutorialIndicatorCheck2.OnCompleteIndicatorRow.AddListener(OnCompleteIndicatorRow2);
        tutorialIndicatorCheck2.OnFillFourSlots.AddListener(OnFillFourIndicators);

        Domino.OnDominoCreated.AddListener(OnSpawnDomino);
        PlayerDominoPlacement.OnDominoReleased.AddListener(OnDropDomino);
        Domino.OnDominoPlacedCorrectly.AddListener(OnFillOneIndicator);
        CameraController.OnFreeLookCameraEnabled.AddListener(OnCascadeEnd);
        CameraController.OnFreeLookCameraDisabled.AddListener(OnCascadeStart);
    }

    public void StartTutorial()
    {
        isTutorialActive = true;
        currentStepIndex = 0;
        UpdateStep();
    }

    private void UpdateStep()
    {
        if (currentStepIndex >= steps.Count)
        {
            CompleteTutorial();
            return;
        }

        var currentStep = steps[currentStepIndex];
        tutorialText.text = currentStep.text;
        if (currentStep.completionCondition == CompletionCondition.ClickButton)
        {
            tutorialButton.gameObject.SetActive(true); // Show the button
        }

        placementEnabled = currentStep.placementEnabled;
        OnTogglePlacementControls.Invoke(placementEnabled);
        PlayerDominoPlacement.placementEnabled = placementEnabled; // Update the PlayerDominoPlacement script
        PlayerDominoPlacement.placementLimited = currentStep.limitedPlacement;
        visible = currentStep.visible;
        if (visible) audioSource.Play(); // Play the pop-up audio

        SetCameraPriority(currentStep.cameraEnabled);

        if (currentStep.worldTarget != null)
        {
            DrawArrowToTarget(currentStep.worldTarget);
        }
        else
        {
            ClearArrows();
        }

        if (currentStep.completionCondition == CompletionCondition.ClickButton) ActivateButton();
    }

    private void SetCameraPriority(bool enable)
    {
        starterCamera.Priority = enable ? 0 : 30;
        OnToggleCameraControls.Invoke(enable);
    }

    private void ActivateButton()
    {
        tutorialButton.gameObject.SetActive(true); // Show the button
        tutorialButton.onClick.RemoveAllListeners();
        tutorialButton.onClick.AddListener(() => NextStep());
    }

    private void Update()
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        // Update arrow positions
        foreach (var arrow in activeArrows)
        {
            if (arrow != null && arrowTargets.TryGetValue(arrow, out var target))
            {
                UpdateArrowPosition(arrow, target);
            }
        }
    }

    private void CheckCompletionCondition(CompletionCondition condition)
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        if (steps[currentStepIndex].completionCondition == condition)
        {
            NextStep();
        }
    }

    private void NextStep()
    {
        currentStepIndex++;
        tutorialButton.gameObject.SetActive(false);
        UpdateStep();
        AnimateTutorialVisibility(visible);
    }

    private void CompleteTutorial()
    {
        isTutorialActive = false;
        ClearArrows();
        tutorialText.text = string.Empty;
        tutorialButton.gameObject.SetActive(false);
        OnTogglePlacementControls.Invoke(true); // Re-enable controls after tutorial
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));
    }

    private void DrawArrowToTarget(GameObject target)
    {
        ClearArrows(); // Clear any existing arrows

        if (uiCanvas == null || target == null) return;

        var indicators = target.GetComponentsInChildren<PlacementIndicator>();
        if (indicators.Length > 0)
        {
            foreach (var indicator in indicators)
            {
                if (indicator.currentState != PlacementIndicator.IndicatorState.Filled) // Only add arrows for unfilled indicators
                {
                    var arrowInstance = Instantiate(arrowSpritePrefab, uiCanvas.transform);
                    activeArrows.Add(arrowInstance);
                    arrowTargets[arrowInstance] = indicator.transform; // Track the target transform
                    UpdateArrowPosition(arrowInstance, indicator.transform);

                    // Listen to the static OnIndicatorFilled event
                    PlacementIndicator.OnIndicatorFilled.AddListener((filledIndicator) =>
                    {
                        if (filledIndicator == indicator) RemoveArrow(arrowInstance);
                    });
                }
            }
        }
        else
        {
            // Fallback: Single arrow pointing to the target's transform
            var arrowInstance = Instantiate(arrowSpritePrefab, uiCanvas.transform);
            activeArrows.Add(arrowInstance);
            arrowTargets[arrowInstance] = target.transform; // Track the target transform
            UpdateArrowPosition(arrowInstance, target.transform);
        }
    }

    private void UpdateArrowPosition(GameObject arrowInstance, Transform target)
    {
        if (arrowInstance == null || target == null || uiCanvas == null) return;

        Vector3 adjustedTargetPosition = target.position;
        adjustedTargetPosition.y += .75f;
        Vector3 targetScreenPosition = mainCamera.WorldToScreenPoint(adjustedTargetPosition);

        RectTransform arrowRectTransform = arrowInstance.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.GetComponent<RectTransform>(),
            targetScreenPosition,
            uiCanvas.worldCamera,
            out Vector2 localPoint);
        arrowRectTransform.localPosition = (Vector3)localPoint + new Vector3(0, Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight, 0);
        arrowRectTransform.rotation = Quaternion.identity;
    }

    private void RemoveArrow(GameObject arrowInstance)
    {
        if (arrowInstance != null)
        {
            activeArrows.Remove(arrowInstance);
            arrowTargets.Remove(arrowInstance); // Remove from the target tracking
            Destroy(arrowInstance);
        }
    }

    private void ClearArrows()
    {
        foreach (var arrow in activeArrows)
        {
            if (arrow != null) Destroy(arrow);
        }
        activeArrows.Clear();
        arrowTargets.Clear(); // Clear the target tracking
    }

    private void AnimateTutorialVisibility(bool isVisible)
    {
        if (isVisible)
        {
            transform.DOScale(0.5f, 0.2f).SetEase(Ease.OutQuad).OnComplete(() =>
            {
                transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
            });
        }
        else
        {
            transform.DOScale(0, 0.2f).SetEase(Ease.OutQuad);
        }
    }

    private void OnCompleteIndicatorRow1()
    {
        CheckCompletionCondition(CompletionCondition.CompleteIndicatorRow1);
    }
    private void OnCompleteIndicatorRow2()
    {
        CheckCompletionCondition(CompletionCondition.CompleteIndicatorRow2);
    }
    private void OnCascadeStart()
    {
        CheckCompletionCondition(CompletionCondition.WaitforCascadeStart);
    }
    private void OnCascadeEnd()
    {
        CheckCompletionCondition(CompletionCondition.WaitForCascadeEnd);
    }

    private void OnSpawnDomino(Domino domino)
    {
        CheckCompletionCondition(CompletionCondition.SpawnADomino);
    }

    private void OnDropDomino(Domino domino)
    {
        CheckCompletionCondition(CompletionCondition.DropADomino);
    }

    private void OnFillOneIndicator(Domino domino)
    {
        CheckCompletionCondition(CompletionCondition.FillOneIndicator);
    }
    private void OnFillFourIndicators()
    {
        CheckCompletionCondition(CompletionCondition.FillFourIndicators);
    }
}