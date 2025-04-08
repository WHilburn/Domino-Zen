using System.Collections;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using DG.Tweening;
using TMPro;
using System.Collections.Generic;

public class TutorialManager : MonoBehaviour
{
    public static TutorialManager Instance { get; private set; }
    private bool isTutorialActive = false;
    private int currentStepIndex = 0;
    public List<TutorialStep> steps;
    public Button tutorialButton;
    public TextMeshProUGUI tutorialText; // Reference to the UI Text element
    public GameObject arrowSpriteInstance; // Instance of the arrow sprite
    private float bobbingSpeed = 3f; // Speed of the bobbing animation
    private float bobbingHeight = 10f; // Height of the bobbing animation
    private bool placementEnabled = false; // Flag to enable/disable controls
    public static UnityEvent<bool> OnTogglePlacementControls = new(); //Events to enable/disable controls
    public static UnityEvent<bool> OnToggleCameraControls = new();
    public Camera mainCamera; // Reference to the main camera
    public Canvas uiCanvas; // Reference to the UI Canvas
    private Transform currentTarget; // Store the current target for the arrow

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
        }
        else
        {
            Destroy(gameObject);
        }
        // tween the scale of the object from 0 to 1 over 0.5 seconds
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        arrowSpriteInstance.SetActive(false); // Hide the arrow sprite initially
    }

    void Start()
    {
        if (steps.Count > 0)
        {
            StartTutorial();
        }

        // Subscribe to TutorialIndicatorCheck's event
        foreach (var indicatorCheck in FindObjectsOfType<TutorialIndicatorCheck>())
        {
            indicatorCheck.OnAllIndicatorsFilled.AddListener(OnCompleteIndicatorRow);
        }

        // Subscribe to other relevant events
        Domino.OnDominoCreated.AddListener(OnSpawnDomino);
        PlayerDominoPlacement.OnDominoReleased.AddListener(OnDropDomino);        
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
        if (currentStep.completionCondition == CompletionCondition.ClickButton){
            tutorialButton.enabled = true;
        }
        
        placementEnabled = currentStep.placementEnabled;
        OnTogglePlacementControls.Invoke(placementEnabled);

        if (currentStep.worldTarget != null)
        {
            DrawArrowToTarget(currentStep.worldTarget);
        }
        else
        {
            ClearArrow();
        }

        SubscribeToCompletionCondition(currentStep.completionCondition);
    }

    private void SubscribeToCompletionCondition(CompletionCondition condition)
    {
        switch (condition)
        {
            case CompletionCondition.CompleteIndicatorRow:
                // Already handled via OnCompleteIndicatorRow
                break;
            case CompletionCondition.SpawnADomino:
                // Handled via OnSpawnDomino
                break;
            case CompletionCondition.DropADomino:
                // Handled via OnDropDomino
                break;
            case CompletionCondition.FillOneIndicator:
                // Logic to handle FillOneIndicator can be added here
                break;
            case CompletionCondition.ClickButton:
                tutorialButton.enabled = true; // Enable the button for clicking
                tutorialButton.onClick.RemoveAllListeners();
                tutorialButton.onClick.AddListener(() => NextStep());
                break;
        }
    }

    private void Update()
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        var currentStep = steps[currentStepIndex];
        // if (currentStep.completionCondition != null && currentStep.completionCondition.Invoke()) 
        // {
        //     NextStep();
        // }
        // Update arrow position if a target is set
        if (currentTarget != null)
        {
            UpdateArrowPosition(currentTarget);
        }
    }

    private void CheckCompletionCondition(CompletionCondition condition)
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        var currentStep = steps[currentStepIndex];
        if (currentStep.completionCondition == condition)
        {
            NextStep();
        }
    }

    private void NextStep()
    {
        currentStepIndex++;
        tutorialButton.enabled = false; // Disable the button after clicking
        UpdateStep();
        transform.DOScale(0.8f, 0.2f).SetEase(Ease.OutQuad).OnComplete(() =>
        {
            transform.DOScale(1f, 0.2f).SetEase(Ease.OutQuad);
        });
    }

    private void CompleteTutorial()
    {
        isTutorialActive = false;
        ClearArrow();
        tutorialText.text = string.Empty;
        tutorialButton.gameObject.SetActive(false);
        OnTogglePlacementControls.Invoke(true); // Re-enable controls after tutorial
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));
    }

    private void DrawArrowToTarget(Transform target)
    {
        if (uiCanvas == null) return;
        arrowSpriteInstance.SetActive(true);

        currentTarget = target; // Set the current target
        UpdateArrowPosition(target);
    }

    private void UpdateArrowPosition(Transform target)
    {
        if (arrowSpriteInstance == null || target == null || uiCanvas == null) return;

        // Convert target position to screen space
        Vector3 adjustedTargetPosition = target.position;
        adjustedTargetPosition.y += 1; // Adjust height for the arrow
        Vector3 targetScreenPosition = mainCamera.WorldToScreenPoint(adjustedTargetPosition);

        arrowSpriteInstance.SetActive(true); // Show the arrow
        // Position the arrow on the canvas
        RectTransform arrowRectTransform = arrowSpriteInstance.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.GetComponent<RectTransform>(),
            targetScreenPosition,
            uiCanvas.worldCamera,
            out Vector2 localPoint);
        arrowRectTransform.localPosition = localPoint;

        // Apply bobbing animation
        float bobbingOffset = Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight;
        arrowRectTransform.localPosition += new Vector3(0, bobbingOffset, 0);

        // Reset rotation since it's hovering
        arrowRectTransform.rotation = Quaternion.identity;
    }

    private void ClearArrow()
    {
        if (arrowSpriteInstance != null)
        {
            arrowSpriteInstance.SetActive(false);
        }
        currentTarget = null; // Reset the current target
    }

    private void OnCompleteIndicatorRow()
    {
        CheckCompletionCondition(CompletionCondition.CompleteIndicatorRow);
    }

    private void OnSpawnDomino(Domino domino)
    {
        NextStep();
    }

    private void OnDropDomino(Domino domino)
    {
        NextStep();
    }
}