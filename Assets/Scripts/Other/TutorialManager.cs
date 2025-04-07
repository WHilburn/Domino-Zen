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
    // private bool isTutorialCompleted = false;
    private int currentStepIndex = 0;
    public List<TutorialStep> steps;

    public Button tutorialButton;
    public TextMeshProUGUI tutorialText; // Reference to the UI Text element
    private LineRenderer tutorialArrow; // Reference to the LineRenderer for the tutorial arrow
    private bool controlsEnabled = false; // Flag to enable/disable controls
    public static UnityEvent<bool> OnToggleControls = new(); //Event to enable/disable controls

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
        // isTutorialComplete = true;
        ClearArrow();
        tutorialText.text = string.Empty;
        tutorialButton.gameObject.SetActive(false);
        OnToggleControls.Invoke(true); // Re-enable controls after tutorial
        transform.DOScale(Vector3.zero, 0.5f).SetEase(Ease.InBack).OnComplete(() => gameObject.SetActive(false));
    }

    private void DrawArrowToTarget(Transform target)
    {
        if (tutorialArrow == null)
        {
            tutorialArrow = gameObject.AddComponent<LineRenderer>();
            tutorialArrow.startWidth = 0.05f;
            tutorialArrow.endWidth = 0.05f;
            tutorialArrow.material = new Material(Shader.Find("Sprites/Default"));
            tutorialArrow.positionCount = 2;
        }

        Vector3 screenPoint = Camera.main.WorldToScreenPoint(target.position);
        Vector3 uiPosition = tutorialText.transform.position;
        tutorialArrow.SetPosition(0, uiPosition);
        tutorialArrow.SetPosition(1, screenPoint);
    }

    private void ClearArrow()
    {
        if (tutorialArrow != null)
        {
            tutorialArrow.positionCount = 0;
        }
    }
}