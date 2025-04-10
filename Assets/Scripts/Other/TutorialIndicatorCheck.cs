using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TutorialIndicatorCheck : MonoBehaviour
{
    public UnityEvent OnCompleteIndicatorRow = new UnityEvent();
    public UnityEvent OnFillFourSlots = new UnityEvent(); // Event to be invoked when four indicators are filled

    private List<PlacementIndicator> childIndicators = new List<PlacementIndicator>();

    private void Start()
    {
        // Get all child PlacementIndicator components
        childIndicators.AddRange(GetComponentsInChildren<PlacementIndicator>());

        // Subscribe to OnIndicatorFilled event for each child indicator
        PlacementIndicator.OnIndicatorFilled.AddListener(OnChildIndicatorFilled);
    }

    private void OnChildIndicatorFilled(PlacementIndicator indicator)
    {
        bool allIndicatorsFilled = true;
        int filledCount = 0;
        foreach (var childIndicator in childIndicators)
        {
            if (childIndicator.currentState != PlacementIndicator.IndicatorState.Filled)
            {
                allIndicatorsFilled = false;
                break;
            } // Check if all indicators are filled
            else
            {
                filledCount++;
            }
            if (filledCount == 4)
            {
                OnFillFourSlots.Invoke(); // Invoke the event when four indicators are filled

            }
        }
        if (allIndicatorsFilled)
        {
            OnCompleteIndicatorRow.Invoke(); // Invoke the event when all indicators are filled
        }
    }
}
