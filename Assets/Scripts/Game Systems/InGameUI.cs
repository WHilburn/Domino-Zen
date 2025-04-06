using TMPro;
using UnityEngine;
using UnityEngine.UI;

public class DominoCounterUI : MonoBehaviour
{
    [SerializeField] private TextMeshProUGUI dominoCountText; // Reference to the UI Text element
    [SerializeField] private TextMeshProUGUI indicatorCountText;
    private int dominoCount = 0;
    private int indicatorCount = 0;

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
