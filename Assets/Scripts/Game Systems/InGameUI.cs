using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

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
    public Button cameraForwardButton;
    public Button cameraLeftButton;
    public Button cameraBackButton;
    public Button cameraRightButton;
    public Button cameraUpButton;
    public Button cameraDownButton;
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
    #endregion

    #region Static Variables
    public static int dominoCount = 0;
    public static int indicatorCount = 0;
    public static bool paused = false; // Static variable to track pause state
    public Texture2D CursorTexture; // Texture for the custom cursor
    
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
            difficultyDropdown.value = (int)GameManager.Instance.gameDifficulty;
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
