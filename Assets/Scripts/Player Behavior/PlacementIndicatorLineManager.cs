using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using DG.Tweening;

public class PlacementIndicatorLineManager
{
    private Vector3 decalPosition;

    private HashSet<PlacementIndicator> FindNearbyPlacementIndicators(float maxDistance)
    {
        HashSet<PlacementIndicator> nearbyIndicators = new HashSet<PlacementIndicator>();
        Collider[] hitColliders = Physics.OverlapSphere(decalPosition, maxDistance);
        foreach (var collider in hitColliders)
        {
            PlacementIndicator indicator = collider.GetComponent<PlacementIndicator>();
            if (indicator != null)
            {
                nearbyIndicators.Add(indicator);
            }
        }
        return nearbyIndicators;
    }

    public void UpdateLinesForIndicators(Vector3 decalPosition, float maxDistance, AnimationCurve alphaCurve)
    {
        this.decalPosition = decalPosition + Vector3.up * 0.5f;
        HashSet<PlacementIndicator> indicators = FindNearbyPlacementIndicators(maxDistance);
        if (indicators == null || indicators.Count == 0) return;

        List<PlacementIndicator> closestIndicators = indicators
            .Where(indicator => indicator != null && indicator.isActiveAndEnabled)
            .OrderBy(indicator => Vector3.Distance(indicator.transform.position, decalPosition))
            .ToList();

        for (int i = 0; i < closestIndicators.Count; i++)
        {
            HandleOutlineAlpha(closestIndicators[i]);
        }
    }

    private void HandleOutlineAlpha(PlacementIndicator indicator)
    {
        indicator.FadeOutline(1f, 0.25f); // Fade out the outline alpha
    }
}