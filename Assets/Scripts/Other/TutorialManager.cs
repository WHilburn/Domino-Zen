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
    private LineRenderer tutorialArrow; // Reference to the LineRenderer for the tutorial arrow
    private bool controlsEnabled = false; // Flag to enable/disable controls
    public static UnityEvent<bool> OnToggleControls = new(); //Event to enable/disable controls
    public Camera mainCamera; // Reference to the main camera

    private Transform currentTarget; // Store the current target for the arrow
    private Material arrowMaterial; // Material for the arrow

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
    }

    void Start()
    {
        if (steps.Count > 0)
        {
            StartTutorial();
        }

        arrowMaterial = new Material(Shader.Find("Sprites/Default"));
        arrowMaterial.mainTexture = Resources.Load<Texture2D>("ArrowTexture"); // Ensure you have an arrow texture in Resources
        arrowMaterial.color = Color.red;
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
        tutorialButton.gameObject.SetActive(currentStep.enableButton);
        controlsEnabled = currentStep.controlsEnabled;
        OnToggleControls.Invoke(controlsEnabled);

        if (currentStep.worldTarget != null)
        {
            DrawArrowToTarget(currentStep.worldTarget);
        }
        else
        {
            ClearArrow();
        }

        if (currentStep.enableButton)
        {
            tutorialButton.onClick.RemoveAllListeners();
            tutorialButton.onClick.AddListener(() => NextStep());
        }
    }

    private void Update()
    {
        if (!isTutorialActive || currentStepIndex >= steps.Count) return;

        var currentStep = steps[currentStepIndex];
        if (currentStep.completionCondition != null && currentStep.completionCondition.Invoke())
        {
            NextStep();
        }
    }
    private void LateUpdate()
    {
        // Update arrow position if a target is set
        if (currentTarget != null)
        {
            UpdateArrowPosition(currentTarget);
        }
    }

    private void NextStep()
    {
        currentStepIndex++;
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
        OnToggleControls.Invoke(true); // Re-enable controls after tutorial
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));
    }

    // Draw the arrow and set the current target
    private void DrawArrowToTarget(Transform target)
    {
        if (tutorialArrow == null)
        {
            tutorialArrow = gameObject.AddComponent<LineRenderer>();
            tutorialArrow.startWidth = 0.01f;
            tutorialArrow.endWidth = 0.05f;
            tutorialArrow.material = arrowMaterial;
            tutorialArrow.startColor = Color.red;
            tutorialArrow.endColor = Color.red;
        }

        currentTarget = target; // Set the current target
        UpdateArrowPosition(target);
    }

    // Update the arrow's position dynamically
    private void UpdateArrowPosition(Transform target)
    {
        // Convert UI position to world space
        Vector3 uiWorldPosition = mainCamera.ScreenToWorldPoint(new Vector3(
            tutorialText.transform.position.x,
            tutorialText.transform.position.y,
            mainCamera.nearClipPlane));

        // Convert target position to world space
        Vector3 targetWorldPosition = target.position;
        targetWorldPosition.y += 0.5f; // Adjust height for the arrow

        tutorialArrow.positionCount = 2;
        tutorialArrow.SetPosition(0, uiWorldPosition);
        tutorialArrow.SetPosition(1, targetWorldPosition);
    }

    // Clear the arrow and reset the target
    private void ClearArrow()
    {
        if (tutorialArrow != null)
        {
            tutorialArrow.positionCount = 0;
        }
        currentTarget = null; // Reset the current target
    }
}