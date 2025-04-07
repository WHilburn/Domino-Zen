using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class InGameUI : MonoBehaviour
{
    public static InGameUI Instance { get; private set; }
    [SerializeField] private TextMeshProUGUI dominoCountText; // Reference to the UI Text element
    [SerializeField] private TextMeshProUGUI indicatorCountText;
    public static int dominoCount = 0;
    public static int indicatorCount = 0;

    void Awake()
    {
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
}
