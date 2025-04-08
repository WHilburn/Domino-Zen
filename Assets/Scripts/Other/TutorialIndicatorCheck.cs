using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;

public class TutorialIndicatorCheck : MonoBehaviour
{
    public UnityEvent<TutorialIndicatorCheck> OnCompleteIndicatorRow = new UnityEvent<TutorialIndicatorCheck>();

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
        foreach (var childIndicator in childIndicators)
        {
            if (childIndicator.currentState != PlacementIndicator.IndicatorState.Filled)
            {
                allIndicatorsFilled = false;
                break;
            }
        }
        if (allIndicatorsFilled)
        {
            OnCompleteIndicatorRow.Invoke(this); // Invoke the event when all indicators are filled
            Debug.Log("All indicators filled!");
        }
    }
}
