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

    public Material opaqueMaterial;
    public Material transparentMaterial;

    void Start()
    {
        meshCollider = GetComponent<MeshCollider>();
        bridgeRenderer = GetComponent<Renderer>();

        // Find child PlacementIndicators (on top of the bridge)
        foreach (Transform child in transform)
        {
            PlacementIndicator childIndicator = child.GetComponent<PlacementIndicator>();
            if (childIndicator != null)
            {
                topOfBridgeIndicators.Add(childIndicator);
            }
        }
        foreach (var indicator in underBridgeIndicators)
        {
            indicator.OnIndicatorFilledInstance.AddListener(OnIndicatorFilled);
            indicator.OnIndicatorEmptiedInstance.AddListener(OnIndicatorEmptied);
        }

        if (underBridgeIndicators.Count == 0)
        {
            Debug.LogWarning("No under bridge indicators found. Please assign them in the inspector.");
            MaterializeBridge();
        }
        else Invoke("MakeBridgeTransparent", 0.05f);
    }

    private void OnIndicatorFilled()
    {
        filledIndicatorsCount++;
        if (filledIndicatorsCount == underBridgeIndicators.Count)
        {
            Invoke("MaterializeBridge", 1f);
        }
        else if (filledIndicatorsCount < underBridgeIndicators.Count)
        {
            MakeBridgeTransparent();
        }
    }

    private void OnIndicatorEmptied()
    {
        filledIndicatorsCount--;
        if (filledIndicatorsCount == underBridgeIndicators.Count)
        {
            Invoke("MaterializeBridge", 1f);
        }
        else if (filledIndicatorsCount < underBridgeIndicators.Count)
        {
            MakeBridgeTransparent();
        }
    }

    private void MakeBridgeTransparent()
    {
        bridgeRenderer.material = transparentMaterial; // Swap to transparent material
        meshCollider.enabled = false;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            Debug.Log("Making bridge transparent");
            childIndicator.FadeOut(false);
            childIndicator.currentState = PlacementIndicator.IndicatorState.Disabled;
        }
    }

    private void MaterializeBridge()
    {
        bridgeRenderer.material = opaqueMaterial; // Swap to opaque material
        meshCollider.enabled = true;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            childIndicator.FadeIn(false);
            childIndicator.currentState = PlacementIndicator.IndicatorState.Empty;
        }
    }
}
