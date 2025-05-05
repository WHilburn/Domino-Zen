using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;
using System.Collections;

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
    public Button startOverButton;
    public Button optionsButton;
    public Button mainMenuButton;
    public Slider volumeSlider;
    public TextMeshProUGUI volumeText;
    public Slider fovSlider;
    public TextMeshProUGUI fovText;
    public Slider mysterySlider;
    public TextMeshProUGUI mysteryText;
    public TMP_Dropdown dominoSoundDropdown;
    public Toggle controlReminderToggle;
    public TMP_Dropdown difficultyDropdown;
    public GameObject buttonPrompt1; // Refers to the buttom prompt reminders that pop up when mousing over interactable items
    public GameObject buttonPrompt2;
    public GameObject buttonPrompt3;
    public GameObject buttonPrompt4;
    public GameObject resetWarning;
    public Image resetCountdown;
    public TextMeshProUGUI resetWarningText;
    public TextMeshProUGUI KeybindText1;
    public TextMeshProUGUI KeybindText2;
    public TextMeshProUGUI KeybindText3;
    public TextMeshProUGUI KeybindText4;
    public TextMeshProUGUI buttonActionText1;
    public TextMeshProUGUI buttonActionText2;
    public TextMeshProUGUI buttonActionText3;
    public TextMeshProUGUI buttonActionText4;
    public TextMeshProUGUI confirmationText;
    public GameObject confirmationPanel;
    public Button confirmButton;
    public Button cancelButton;
    #endregion

    #region Static Variables
    public static int filledIndicatorCount = 0;
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
        filledIndicatorCount = 0; // Initialize indicator count

        // Wire up buttons
        pauseButton.onClick.AddListener(HandlePauseButton);
        unpauseButton.onClick.AddListener(HandlePauseButton);
        optionsButton.onClick.AddListener(ToggleOptionsPanel); // Update to use ToggleOptionsPanel
        mainMenuButton.onClick.AddListener(mainMenuButtonPressed); // Wire up main menu button

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

        // Wire up controlReminderToggle
        controlReminderToggle.onValueChanged.AddListener((bool isOn) =>
        {
            UpdateButtonPrompts(); // Refresh button prompts visibility based on toggle state
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
        if (confirmationPanel != null)
        {
            confirmationPanel.SetActive(false); // Hide confirmation panel at start
            startOverButton.onClick.AddListener(startOverButtonPressed); // Wire up start over button
        }
        StartCoroutine(LoadSettings()); // Load settings from PlayerPrefs
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
        PlacementIndicator.OnIndicatorFilled.AddListener(HandleIndicatorFilled);
        PlacementIndicator.OnIndicatorEmptied.AddListener(HandleIndicatorEmptied);
    }

    void Update()
    {
        if (Input.GetButtonDown("Menu") && PlayerDominoPlacement.heldDomino == null)
        {
            HandlePauseButton();
        }
        UpdateResetWarning();
        UpdateButtonPrompts();
        UpdateCountText();
    }

    public void LoadMainMenu()
    {
        SceneLoader.Instance.StartSceneTransitionCoroutine("Main Menu");
    }

    public void mainMenuButtonPressed()
    {
        if (GameManager.filledIndicators > 0) // Check if there is progress
        {
            confirmationPanel.SetActive(true); // Show confirmation panel
            DisableAllButtons(true); // Disable all other buttons
            confirmationText.text = "You have made progress. Are you sure you want to return to the main menu? Progress will be saved."; // Update confirmation text
            confirmButton.onClick.RemoveAllListeners(); // Remove all listeners from the confirm button
            confirmButton.onClick.AddListener(() =>
            {
                GameManager.Instance.SaveLevelStats(); // Save progress
                SceneLoader.Instance.StartSceneTransitionCoroutine("Main Menu"); // Load main menu
                confirmationPanel.SetActive(false); // Hide confirmation panel
                DisableAllButtons(false); // Re-enable all buttons
            });
            cancelButton.onClick.RemoveAllListeners(); // Remove all listeners from the cancel button
            cancelButton.onClick.AddListener(() =>
            {
                confirmationPanel.SetActive(false); // Hide confirmation panel
                DisableAllButtons(false); // Re-enable all buttons
            });
        }
        else
        {
            SceneLoader.Instance.StartSceneTransitionCoroutine("Main Menu"); // Load main menu directly
        }
    }

    private void UpdateResetWarning()
    {
        if (DominoResetManager.Instance.currentState == DominoResetManager.ResetState.ResetUpcoming &&
            !CameraController.isTracking &&
            DominoResetManager.timeUntilReset <= DominoResetManager.resetDelay - 0.25f &&
            !GameManager.levelComplete)
        {
            resetWarning.SetActive(true);
            resetCountdown.fillAmount = 1;// - (DominoResetManager.timeUntilReset / (DominoResetManager.resetDelay - 0.17f)); // Update countdown fill amount
            resetCountdown.rectTransform.Rotate(Vector3.forward, 90 * Time.deltaTime);
            resetWarningText.text = "Reset triggered";
        }
        else if (DominoResetManager.Instance.currentState == DominoResetManager.ResetState.Resetting &&
        !GameManager.levelComplete)
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
        if (!controlReminderToggle.isOn || !PlayerDominoPlacement.Instance.ControlsActive()) // Hide all button prompts if toggle is off or controls disabled
        {
            buttonPrompt1.SetActive(false);
            buttonPrompt2.SetActive(false);
            buttonPrompt3.SetActive(false);
            buttonPrompt4.SetActive(false);
            return;
        }

        // Retrieve input bindings, TEMP SOLUTION
        string interactKey = "C";
        string rotatePositiveKey = "Q";
        string rotateNegativeKey = "E";
        string raiseDominoKey = "R";
        string lowerDominoKey = "F";
        string cancelKey = "Escape";
        string spawnAndDropDominoKey = "Space";
        string pickUpDominoKey = "Left Mouse";
        string aimCameraKey = "Right Mouse";
        string moveCameraKey = " W \nASD";


        // Update based on certain game states
        if (PlayerDominoPlacement.heldDomino != null)
        {
            buttonPrompt1.SetActive(true);
            buttonPrompt2.SetActive(true);
            buttonPrompt3.SetActive(true);
            buttonPrompt4.SetActive(true);
            KeybindText1.text = $"{rotatePositiveKey}/{rotateNegativeKey}";
            buttonActionText1.text = "Rotate";
            KeybindText2.text = spawnAndDropDominoKey;
            buttonActionText2.text = "Drop";
            KeybindText3.text = cancelKey;
            buttonActionText3.text = "Delete";
            KeybindText4.text = $"{raiseDominoKey}/{lowerDominoKey}";
            buttonActionText4.text = "Raise/\nLower";
            return;
        }
        if (PlayerObjectMovement.isMovingObject)
        {
            buttonPrompt1.SetActive(true);
            buttonPrompt2.SetActive(true);
            buttonPrompt3.SetActive(false);
            buttonPrompt4.SetActive(false);
            KeybindText1.text = pickUpDominoKey;
            buttonActionText1.text = "Place\nObject";
            KeybindText2.text = cancelKey;
            buttonActionText2.text = "Cancel";
            return;
        }
        // Perform a raycast from the mouse position
        Ray ray = mainCamera.ScreenPointToRay(Input.mousePosition);
        if (Physics.Raycast(ray, out RaycastHit hit))
        {
            // Check if the object under the cursor is a Domino
            if (hit.collider.GetComponent<Domino>() != null && PlayerDominoPlacement.Instance.placementEnabled)
            {
                buttonPrompt1.SetActive(true);
                buttonPrompt2.SetActive(true);
                buttonPrompt3.SetActive(false);
                buttonPrompt4.SetActive(false);
                KeybindText1.text = pickUpDominoKey;
                buttonActionText1.text = "Pick Up";
                KeybindText2.text = interactKey;
                buttonActionText2.text = "Knock Over";
            }
            // Check if the object under the cursor is a Bucket
            else if (hit.collider.GetComponent<Bucket>() != null && PlayerDominoPlacement.Instance.placementEnabled)
            {
                buttonPrompt1.SetActive(true);
                buttonPrompt2.SetActive(true);
                buttonPrompt3.SetActive(false);
                buttonPrompt4.SetActive(false);
                KeybindText1.text = spawnAndDropDominoKey;
                buttonActionText1.text = "Pick Up\nDomino";
                KeybindText2.text = interactKey;
                buttonActionText2.text = "Relocate";
            }
            else
            {
                // Disable button prompts if no relevant object is under the cursor
                buttonPrompt1.SetActive(true);
                buttonPrompt2.SetActive(true);
                buttonPrompt3.SetActive(true);
                if (PlayerDominoPlacement.Instance.bucketModeEnabled)
                buttonPrompt4.SetActive(false);
                else
                buttonPrompt4.SetActive(true);
                KeybindText1.text = moveCameraKey;
                buttonActionText1.text = "Move\nCamera";
                KeybindText2.text = aimCameraKey;
                buttonActionText2.text = "Aim\nCamera";
                KeybindText3.text = $"{raiseDominoKey}/{lowerDominoKey}";
                buttonActionText3.text = "Camera\nUp/Down";
                KeybindText4.text = spawnAndDropDominoKey;
                buttonActionText4.text = "Spawn\nDomino";
            }
        }
        else
        {
            // Disable button prompts if no object is under the cursor
            buttonPrompt1.SetActive(false);
            buttonPrompt2.SetActive(false);
            buttonPrompt3.SetActive(false);
            buttonPrompt4.SetActive(false);
        }
    }
    #endregion

    #region Event Handlers
    public void startOverButtonPressed()
    {
        confirmationPanel.SetActive(true); // Show confirmation panel
        DisableAllButtons(true); // Disable all other buttons
        confirmationText.text = "Are you sure you want to reset the level? All progress will be lost."; // Update confirmation text
        confirmButton.onClick.RemoveAllListeners(); // Remove all listeners from the confirm button
        confirmButton.onClick.AddListener(() =>
        {
            GameManager.Instance.ResetLevel(); // Reset the level
            confirmationPanel.SetActive(false); // Hide confirmation panel
            DisableAllButtons(false); // Re-enable all buttons
        });
        cancelButton.onClick.RemoveAllListeners(); // Remove all listeners from the cancel button
        cancelButton.onClick.AddListener(() =>
        {
            confirmationPanel.SetActive(false); // Hide confirmation panel
            DisableAllButtons(false); // Re-enable all buttons
        });
    }

    private void HandleIndicatorFilled(PlacementIndicator indicator)
    {
        if (filledIndicatorCount == 0) return; // Ensure the indicator count is valid
        filledIndicatorCount++;
    }

    private void HandleIndicatorEmptied(PlacementIndicator indicator)
    {
        filledIndicatorCount--;
    }
    #endregion

    #region UI Updates
    private void UpdateCountText()
    {
        if (dominoCountText != null)
        {
            dominoCountText.text = $"x {DominoResetManager.Instance.allDominoes.Count}";
            indicatorCountText.text = $"x {GameManager.Instance.allIndicators.Count - GameManager.filledIndicators}"; // Update indicator count text
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

    public void HandlePauseButton()
    {
        GameManager.gamePaused = !GameManager.gamePaused; // Toggle the pause state
        DismissConfirmationPanel();
        TogglePauseMenu(GameManager.gamePaused); // Show or hide the pause menu
    }
    public void TogglePauseMenu(bool on)
    {
        if (pauseMenu != null)
        {
            pauseMenu.SetActive(on); // Toggle pause menu visibility
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

    #region Helper Methods
    private void DisableAllButtons(bool disable)
    {
        confirmButton.interactable = disable; // Ensure confirm and cancel buttons remain interactable
        cancelButton.interactable = disable;
        pauseButton.interactable = !disable;
        // unpauseButton.interactable = !disable;
        startOverButton.interactable = !disable;
        optionsButton.interactable = !disable;
        mainMenuButton.interactable = !disable;
        fovSlider.interactable = !disable;
        volumeSlider.interactable = !disable;
        dominoSoundDropdown.interactable = !disable;
        difficultyDropdown.interactable = !disable;
        controlReminderToggle.interactable = !disable;
    }

    public void DismissConfirmationPanel()
    {
        confirmationPanel.SetActive(false); // Hide confirmation panel
        DisableAllButtons(false); // Re-enable all buttons
    }
    #endregion

    #region Settings Persistence
    public void SaveSettings()
    {
        PlayerPrefs.SetFloat("Volume", volumeSlider.value);
        PlayerPrefs.SetFloat("FOV", fovSlider.value);
        PlayerPrefs.SetInt("ControlReminder", controlReminderToggle.isOn ? 1 : 0);
        PlayerPrefs.SetInt("DominoSound", dominoSoundDropdown.value);
        PlayerPrefs.Save();
    }

    public IEnumerator LoadSettings()
    {
        yield return null;
        if (PlayerPrefs.HasKey("Volume"))
        {
            float volume = PlayerPrefs.GetFloat("Volume");
            volumeSlider.value = volume;
            UpdateVolume(volume);
        }

        if (PlayerPrefs.HasKey("FOV"))
        {
            float fov = PlayerPrefs.GetFloat("FOV");
            fovSlider.value = fov;
            UpdateFOV(fov);
        }

        if (PlayerPrefs.HasKey("ControlReminder"))
        {
            bool controlReminder = PlayerPrefs.GetInt("ControlReminder") == 1;
            controlReminderToggle.isOn = controlReminder;
        }

        if (PlayerPrefs.HasKey("DominoSound"))
        {
            int dominoSound = PlayerPrefs.GetInt("DominoSound");
            dominoSoundDropdown.value = dominoSound;
            dominoSoundDropdown.RefreshShownValue();
            if (DominoSoundManager.Instance != null)
            {
                DominoSoundManager.Instance.SetDominoSound(dominoSound);
            }
        }
    }
    #endregion

    void OnApplicationQuit()
    {
        SaveSettings(); // Save settings when the application quits
    }
}
