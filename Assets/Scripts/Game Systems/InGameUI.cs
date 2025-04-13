using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using DG.Tweening;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI dominoCountText; // Reference to the UI Text element
    [SerializeField] private TextMeshProUGUI indicatorCountText;
    [SerializeField] private Canvas canvas; // Reference to the UI canvas
    [SerializeField] private TextMeshProUGUI floatingTextPrefab; // Prefab for floating text
    public static int dominoCount = 0;
    public static int indicatorCount = 0;
    public Camera mainCamera; // Reference to the main camera
    public Button cameraForwardButton;
    public Button cameraLeftButton;
    public Button cameraBackButton;
    public Button cameraRightButton;
    public Button cameraUpButton;
    public Button cameraDownButton;
    // public Button rotateLeftButton; //Unused for now
    // public Button rotateRightButton;
    // public Button openMenuButton;

    void Awake()
    {
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
    }

    private void OnEnable()
    {
        Domino.OnDominoCreated.AddListener(HandleDominoCreated);
        Domino.OnDominoDeleted.AddListener(HandleDominoDeleted);
        indicatorCount = GameObject.FindGameObjectsWithTag("IndicatorTag").Length;
        PlacementIndicator.OnIndicatorFilled.AddListener(HandleIndicatorFilled);
        PlacementIndicator.OnIndicatorEmptied.AddListener(HandleIndicatorEmptied);
    }

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

    private void UpdateCountText()
    {
        if (dominoCountText != null)
        {
            dominoCountText.text = $"Dominoes Placed: {dominoCount}";
            indicatorCountText.text = $"Indicators Remaining: {indicatorCount}";
        }
    }

    public void CreateFloatingText(string text, Vector3 worldPosition, float scale, float fadeDuration, bool floatUpwards = false)
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
}
