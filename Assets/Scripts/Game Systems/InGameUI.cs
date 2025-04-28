using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using UnityEngine.InputSystem;
using TMPro.Examples;

public class InGameUI : MonoBehaviour
{
    #region Singleton
    public static InGameUI Instance { get; private set; }
    #endregion

    #region Serialized Fields
    [SerializeField] private TextMeshProUGUI dominoCountText; // Reference to the UI Text element
    [SerializeField] private TextMeshProUGUI indicatorCountText;
    [SerializeField] private Canvas canvas; // Reference to the UI canvas
    [SerializeField] private TextMeshProUGUI floatingTextPrefab; // Prefab for floating text
    [SerializeField] private RectTransform buttonPanel; // Reference to the three-button panel
    [SerializeField] private RectTransform optionsPanelRect; // Reference to the options panel RectTransform
    [SerializeField] private float animationDuration = 0.5f; // Duration of the animation
    public Camera mainCamera; // Reference to the main camera
    public GameObject pauseMenu; // For enabling and disabling the pause menu
    public GameObject optionsPanel;
    public Button pauseButton;
    public Button unpauseButton;
    public Button optionsButton;
    public Button mainMenuButton;
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;
    public Slider fovSlider;
    public TextMeshProUGUI fovText;
    public Slider mysterySlider;
    public TextMeshProUGUI mysteryText;
    public TMP_Dropdown dominoSoundDropdown;
    public TMP_Dropdown difficultyDropdown;
    public GameObject buttonPrompt1; // Refers to the buttom prompt reminders that pop up when mousing over interactable items
    public GameObject buttonPrompt2;
    public GameObject buttonPrompt3;
    public GameObject resetWarning;
    public Image resetCountdown;
    public TextMeshProUGUI resetWarningText;
    public TextMeshProUGUI KeybindText1;
    public TextMeshProUGUI KeybindText2;
    public TextMeshProUGUI KeybindText3;
    public TextMeshProUGUI buttonActionText1;
    public TextMeshProUGUI buttonActionText2;
    public TextMeshProUGUI buttonActionText3;
    #endregion

    #region Static Variables
    public static int dominoCount = 0;
    public static int indicatorCount = 0;
    public static bool paused = false; // Static variable to track pause state
    public Texture2D CursorTexture; // Texture for the custom cursor
    public PlayerControls playerControls; // Reference to the PlayerControls input actions
    #endregion

    #region Unity Methods
    void Awake()
    {
        // Set the game's mouse cursor to half size
        // Cursor.SetCursor(CursorTexture, Vector2.zero, CursorMode.Auto);
        // Cursor.SetCursor(CursorTexture, new Vector2(CursorTexture.width / 8, CursorTexture.height / 8), CursorMode.Auto);
        // Ensure only one instance of InGameUI exists
        if (Instance != null && Instance != this)
        {
            Destroy(gameObject);
            return;
        }
        Instance = this;
        playerControls = new PlayerControls(); // Initialize the PlayerControls input actions

        // Ensure there is an Event System in the scene
        if (FindObjectOfType<EventSystem>() == null)
        {
            GameObject eventSystem = new GameObject("EventSystem");
            eventSystem.AddComponent<EventSystem>();
            eventSystem.AddComponent<StandaloneInputModule>();
        }
        dominoCount = 0; // Initialize domino count
        indicatorCount = 0; // Initialize indicator count

        // Wire up buttons
        pauseButton.onClick.AddListener(TogglePauseMenu);
        unpauseButton.onClick.AddListener(TogglePauseMenu);
        optionsButton.onClick.AddListener(ToggleOptionsPanel); // Update to use ToggleOptionsPanel

        // Wire up sliders
        volumeSlider.onValueChanged.AddListener(UpdateVolume);
        fovSlider.onValueChanged.AddListener(UpdateFOV);

        // Wire up dropdown
        dominoSoundDropdown.onValueChanged.AddListener((int value) =>
        {
            if (DominoSoundManager.Instance != null)
            {
                DominoSoundManager.Instance.SetDominoSound(value);
            }
        });

        difficultyDropdown.onValueChanged.AddListener((int value) =>
        {
            if (GameManager.Instance != null)
            {
                GameManager.GameDifficulty difficulty = (GameManager.GameDifficulty)value;
                GameManager.Instance.SetGameDifficulty(difficulty);
            }
        });

        // Disable pause menu and options panel at start
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(false);
        }
        if (optionsPanel != null)
        {
            optionsPanelRect.sizeDelta = new Vector2(0, optionsPanelRect.sizeDelta.y); // Set options panel size to 0
            optionsPanel.SetActive(false);
        }
        Invoke(nameof(InitializeDropdownValues), 0.05f); // Initialize dropdown values after a short delay

        // Set the initial size of the button panel
        if (buttonPanel != null && pauseMenu != null)
        {
            float totalWidth = pauseMenu.GetComponent<RectTransform>().rect.width;
            buttonPanel.sizeDelta = new Vector2(totalWidth / 3, buttonPanel.sizeDelta.y);
        }
    }

    private void InitializeDropdownValues()
    {
        // Initialize difficulty dropdown to the current difficulty
        if (GameManager.Instance != null && difficultyDropdown != null)
        {
            difficultyDropdown.value = (int)GameManager.gameDifficulty;
            difficultyDropdown.RefreshShownValue(); // Refresh the dropdown to display the correct value
        }
    }

    private void OnEnable()
    {
        Domino.OnDominoCreated.AddListener(HandleDominoCreated);
        Domino.OnDominoDeleted.AddListener(HandleDominoDeleted);
        indicatorCount = GameObject.FindGameObjectsWithTag("IndicatorTag").Length;
        PlacementIndicator.OnIndicatorFilled.AddListener(HandleIndicatorFilled);
        PlacementIndicator.OnIndicatorEmptied.AddListener(HandleIndicatorEmptied);
    }

    void Update()
    {
        if (Input.GetButtonDown("Menu")) // Poll the input manager for the "Menu" input
        {
            TogglePauseMenu();
        }

        UpdateResetWarning();
        UpdateButtonPrompts();
    }

    private void UpdateResetWarning()
    {
        if (DominoResetManager.Instance.currentState == DominoResetManager.ResetState.ResetUpcoming &&
            !CameraController.isTracking &&
            DominoResetManager.timeUntilReset <= DominoResetManager.resetDelay - 0.17f)
        {
            resetWarning.SetActive(true);
            resetCountdown.fillAmount = 1;// - (DominoResetManager.timeUntilReset / (DominoResetManager.resetDelay - 0.17f)); // Update countdown fill amount
            resetCountdown.rectTransform.Rotate(Vector3.forward, 90 * Time.deltaTime);
            resetWarningText.text = "Reset triggered";
        }
        else if (DominoResetManager.Instance.currentState == DominoResetManager.ResetState.Resetting)
        {
            resetWarning.SetActive(true);
            resetCountdown.fillAmount = 1; // Reset fill amount to 0
            resetCountdown.rectTransform.Rotate(Vector3.forward, 360 * Time.deltaTime);
            resetWarningText.text = "Resetting..."; // Update reset warning text
        }
        else
        {
            resetWarning.SetActive(false); // Hide reset warning otherwise
        }
    }

    private void UpdateButtonPrompts()
    {
        // Retrieve input bindings, TEMP SOLUTION
        string interactKey = "C";
        string rotatePositiveKey = "Q";
        string rotateNegativeKey = "E";
        string cancelKey = "Escape";
        string spawnAndDropDominoKey = "Space";
        string pickUpDominoKey = "Left Mouse";

        if (paused || 
            DominoResetManager.Instance != null && 
            DominoResetManager.Instance.currentState != DominoResetManager.ResetState.Idle)
        {
            buttonPrompt1.SetActive(false); // Hide button prompts if not paused or in reset state
            buttonPrompt2.SetActive(false);
            buttonPrompt3.SetActive(false);
            return;
        }

        // Update based on certain game states
        if (PlayerDominoPlacement.heldDomino != null)
        {
            buttonPrompt1.SetActive(true);
            buttonPrompt2.SetActive(true);
            buttonPrompt3.SetActive(true);
            KeybindText1.text = $"{rotatePositiveKey}/{rotateNegativeKey}";
            buttonActionText1.text = "Rotate";
            KeybindText2.text = spawnAndDropDominoKey;
            buttonActionText2.text = "Drop";
            KeybindText3.text = cancelKey;
            buttonActionText3.text = "Delete";
            return;
        }
        if (PlayerObjectMovement.isMovingObject)
        {
            buttonPrompt1.SetActive(true);
            buttonPrompt2.SetActive(true);
            buttonPrompt3.SetActive(false);
            KeybindText1.text = pickUpDominoKey;
            buttonActionText1.text = "Place Object";
            KeybindText2.text = cancelKey;
            buttonActionText2.text = "Cancel";
            return;
        }
        // Perform a raycast from the mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the object under the cursor is a Domino
            if (hit.collider.GetComponent<Domino>() != null && PlayerDominoPlacement.placementEnabled)
            {
                buttonPrompt1.SetActive(true);
                buttonPrompt2.SetActive(true);
                buttonPrompt3.SetActive(false);
                KeybindText1.text = pickUpDominoKey;
                buttonActionText1.text = "Pick Up";
                KeybindText2.text = interactKey;
                buttonActionText2.text = "Knock Over";
            }
            // Check if the object under the cursor is a Bucket
            else if (hit.collider.GetComponent<Bucket>() != null && PlayerDominoPlacement.placementEnabled)
            {
                buttonPrompt1.SetActive(true);
                buttonPrompt2.SetActive(true);
                buttonPrompt3.SetActive(false);
                KeybindText1.text = spawnAndDropDominoKey;
                buttonActionText1.text = "Pick Up Domino";
                KeybindText2.text = interactKey;
                buttonActionText2.text = "Relocate";
            }
            else
            {
                // Disable button prompts if no relevant object is under the cursor
                buttonPrompt1.SetActive(false);
                buttonPrompt2.SetActive(false);
                buttonPrompt3.SetActive(false);
            }
        }
        else
        {
            // Disable button prompts if no object is under the cursor
            buttonPrompt1.SetActive(false);
            buttonPrompt2.SetActive(false);
            buttonPrompt3.SetActive(false);
        }
    }
    #endregion

    #region Event Handlers
    private void HandleDominoCreated(Domino domino)
    {
        if (dominoCountText == null) return; // Ensure the text reference is valid
        dominoCount++;
        UpdateCountText();
    }

    private void HandleDominoDeleted(Domino domino)
    {
        dominoCount--;
        UpdateCountText();
    }

    private void HandleIndicatorFilled(PlacementIndicator indicator)
    {
        if (indicatorCount == 0) return; // Ensure the indicator count is valid
        indicatorCount--;
        UpdateCountText();
    }

    private void HandleIndicatorEmptied(PlacementIndicator indicator)
    {
        indicatorCount++;
        UpdateCountText();
    }
    #endregion

    #region UI Updates
    private void UpdateCountText()
    {
        if (dominoCountText != null)
        {
            dominoCountText.text = $"x {dominoCount}";
            indicatorCountText.text = $"x {indicatorCount}";
        }
    }

    private void UpdateVolume(float value)
    {
        if (DominoSoundManager.Instance != null)
        {
            DominoSoundManager.Instance.SetGlobalVolume(value); // Update global volume in sound manager
        }
        if (volumeText != null)
        {
            volumeText.text = $"{Mathf.RoundToInt(value * 100)}%"; // Update volume text
        }
    }

    private void UpdateFOV(float value)
    {
        if (PlayerCameraController.Instance != null)
        {
            PlayerCameraController.Instance.setCameraFOV(value); // Update camera FOV
        }
        if (fovText != null)
        {
            fovText.text = $"{Mathf.RoundToInt(value)}°"; // Update FOV text with degree symbol
        }
    }
    #endregion

    #region UI Animations
    private void TogglePauseMenu()
    {
        paused = !paused; // Toggle the pause state
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(paused); // Toggle pause menu visibility
        }
    }

    private void ToggleOptionsPanel()
    {
        if (optionsPanel == null || buttonPanel == null) return;

        bool isOptionsActive = optionsPanel.activeSelf;
        optionsPanel.SetActive(true); // Ensure the options panel is active for animation
        float totalWidth = pauseMenu.GetComponent<RectTransform>().rect.width;
        HorizontalLayoutGroup layoutGroup = pauseMenu.GetComponent<HorizontalLayoutGroup>();

        if (layoutGroup == null)
        {
            Debug.LogError("HorizontalLayoutGroup is null. Ensure it is attached to the pause menu.");
            return;
        }

        // Animate the panels
        if (isOptionsActive)
        {
            buttonPanel.DOSizeDelta(new Vector2(totalWidth / 3, buttonPanel.sizeDelta.y), animationDuration); // Contract button panel
            optionsPanelRect.DOSizeDelta(new Vector2(0, optionsPanelRect.sizeDelta.y), animationDuration)
                .OnComplete(() => optionsPanel.SetActive(false)); // Collapse options panel and deactivate
            optionsPanelRect.DOScaleX(0, animationDuration);
            DOTween.To(() => layoutGroup.spacing, x => layoutGroup.spacing = x, 0, animationDuration)
                .OnUpdate(() =>
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(pauseMenu.GetComponent<RectTransform>()); // Force layout rebuild
                });
        }
        else
        {
            buttonPanel.DOSizeDelta(new Vector2(totalWidth / 3, buttonPanel.sizeDelta.y), animationDuration); // Expand button panel
            optionsPanelRect.DOSizeDelta(new Vector2((totalWidth * 2) / 4, optionsPanelRect.sizeDelta.y), animationDuration); // Expand options panel
            optionsPanelRect.DOScaleX(1, animationDuration);
            DOTween.To(() => layoutGroup.spacing, x => layoutGroup.spacing = x, 100, animationDuration)
                .OnUpdate(() =>
                {
                    LayoutRebuilder.ForceRebuildLayoutImmediate(pauseMenu.GetComponent<RectTransform>()); // Force layout rebuild
                });
        }

        // Log final spacing value after tween
        DOTween.Sequence().AppendInterval(animationDuration).OnComplete(() =>
        {
            // Debug.Log($"Final spacing: {layoutGroup.spacing}");
        });
    }

    public void CreateFloatingWorldText(string text, Vector3 worldPosition, float scale, float fadeDuration, bool floatUpwards = false)
    {
        if (floatingTextPrefab == null || canvas == null) return;

        // Instantiate the floating text
        TextMeshProUGUI floatingText = Instantiate(floatingTextPrefab, canvas.transform);
        floatingText.text = text;
        floatingText.transform.localScale = Vector3.one * scale;

        // Convert world position to canvas position
        Vector2 screenPosition = mainCamera.WorldToScreenPoint(worldPosition);
        RectTransformUtility.ScreenPointToLocalPointInRectangle(
            canvas.GetComponent<RectTransform>(), 
            screenPosition, 
            canvas.worldCamera, 
            out Vector2 canvasPosition
        );
        floatingText.rectTransform.anchoredPosition = canvasPosition;

        // Animate the text
        Sequence sequence = DOTween.Sequence();
        if (floatUpwards)
        {
            sequence.Append(floatingText.rectTransform.DOAnchorPosY(canvasPosition.y + 50f, fadeDuration));
        }
        sequence.Join(floatingText.DOFade(0, fadeDuration))
                .OnComplete(() => Destroy(floatingText.gameObject));
    }
    #endregion
}
