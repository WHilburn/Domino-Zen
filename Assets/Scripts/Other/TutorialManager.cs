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
    public float arrowOffset = 1.5f; // Offset for the arrow above the target
    private bool isTutorialActive = false;
    private bool isTutorialCompleted = false;
    private int currentStepIndex = 0;
    public TutorialStep[] steps;

    public Button tutorialButton;
    private LineRenderer tutorialArrow; // Reference to the LineRenderer for the tutorial arrow
    private bool controlsEnabled = false; // Flag to enable/disable controls
    public static UnityEvent<bool> OnToggleControls = new(); //Event to enable/disable controls

    void Awake()
    {
        if (Instance == null)
        {
            Instance = this;
            DontDestroyOnLoad(gameObject);
        }
        else
        {
            Destroy(gameObject);
        }
        // tween the scale of the object from 0 to 1 over 0.5 seconds
        transform.localScale = Vector3.zero;
        transform.DOScale(Vector3.one, 0.5f).SetEase(Ease.OutBack);
    }
}