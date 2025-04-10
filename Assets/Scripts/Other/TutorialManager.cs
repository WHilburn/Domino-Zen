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
    public GameObject arrowSpriteInstance; // Instance of the arrow sprite
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

    void Awake()
    {
        if (Instance != null && Instance != this)
        {
            Destroy(Instance.gameObject); // Destroy the previous instance if it exists
        }
        Instance = this;
        
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
        arrowSpriteInstance?.SetActive(false); // Hide the arrow sprite initially
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
        visible = currentStep.visible;

        SetCameraPriority(currentStep.cameraEnabled);

        if (currentStep.worldTarget != null)
        {
            DrawArrowToTarget(currentStep.worldTarget);
        }
        else
        {
            ClearArrow();
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
        // Update arrow position if a target is set
        if (currentTarget != null)
        {
            UpdateArrowPosition(currentTarget);
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

        Vector3 adjustedTargetPosition = target.position;
        adjustedTargetPosition.y += .75f;
        Vector3 targetScreenPosition = mainCamera.WorldToScreenPoint(adjustedTargetPosition);

        RectTransform arrowRectTransform = arrowSpriteInstance.GetComponent<RectTransform>();
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            uiCanvas.GetComponent<RectTransform>(),
            targetScreenPosition,
            uiCanvas.worldCamera,
            out Vector2 localPoint);
        arrowRectTransform.localPosition = (Vector3)localPoint + new Vector3(0, Mathf.Sin(Time.time * bobbingSpeed) * bobbingHeight, 0);
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