using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bridge : MonoBehaviour
{
    private MeshCollider meshCollider;
    private Renderer bridgeRenderer;
    public List<PlacementIndicator> underBridgeIndicators = new();
    private HashSet<PlacementIndicator> topOfBridgeIndicators = new();
    private int filledIndicatorsCount = 0;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        bridgeRenderer = GetComponent<Renderer>();
        PlacementIndicator.OnIndicatorFilled.AddListener(OnIndicatorFilled);
        PlacementIndicator.OnIndicatorEmptied.AddListener(OnIndicatorEmptied);

        // Find child PlacementIndicators (on top of the bridge)
        foreach (Transform child in transform)
        {
            PlacementIndicator childIndicator = child.GetComponent<PlacementIndicator>();
            if (childIndicator != null)
            {
                topOfBridgeIndicators.Add(childIndicator);
            }
        }
        if (underBridgeIndicators.Count == 0)
        {
            Debug.LogWarning("No under bridge indicators found. Please assign them in the inspector.");
        }
        else Invoke("MakeBridgeTransparent",0.05f);
    }

    private void OnIndicatorFilled(PlacementIndicator indicator)
    {
        if (underBridgeIndicators.Contains(indicator))
        {
            filledIndicatorsCount++;
        }
        if (filledIndicatorsCount == underBridgeIndicators.Count)
        {
            // All indicators are filled, make the bridge transparent
            MaterializeBridge();
        }
        else if (filledIndicatorsCount < underBridgeIndicators.Count)
        {
            MakeBridgeTransparent();
        }
    }

    private void OnIndicatorEmptied(PlacementIndicator indicator)
    {
        if (underBridgeIndicators.Contains(indicator))
        {
            filledIndicatorsCount--;
        }
        if (filledIndicatorsCount == underBridgeIndicators.Count)
        {
            // All indicators are filled, make the bridge transparent
            MaterializeBridge();
        }
        else if (filledIndicatorsCount < underBridgeIndicators.Count)
        {
            MakeBridgeTransparent();
        }
    }

    private void MakeBridgeTransparent()
    {
        bridgeRenderer.material.color = new Color(bridgeRenderer.material.color.r, bridgeRenderer.material.color.g, bridgeRenderer.material.color.b, 0.5f);
        meshCollider.enabled = false;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            // childIndicator.gameObject.SetActive(false);
            childIndicator.FadeOut(false);
        }
    }

    private void MaterializeBridge()
    {
        bridgeRenderer.material.color = new Color(bridgeRenderer.material.color.r, bridgeRenderer.material.color.g, bridgeRenderer.material.color.b, 1f);
        meshCollider.enabled = true;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            childIndicator.gameObject.SetActive(true);
            childIndicator.FadeIn(false);
        }
    }

    private void ResetBridge()
    {
        bridgeRenderer.material.color = new Color(bridgeRenderer.material.color.r, bridgeRenderer.material.color.g, bridgeRenderer.material.color.b, 1f);
        meshCollider.enabled = true;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            childIndicator.gameObject.SetActive(true);
        }
    }
}
