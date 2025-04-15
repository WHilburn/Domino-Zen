using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class Bridge : MonoBehaviour
{
    private BoxCollider boxCollider;
    private MeshCollider meshCollider;
    private Renderer bridgeRenderer;
    private HashSet<PlacementIndicator> underBridgeIndicators = new();
    private HashSet<PlacementIndicator> topOfBridgeIndicators = new();
    private int filledIndicatorsCount = 0;

    void Start()
    {
        boxCollider = GetComponent<BoxCollider>();
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

        StartCoroutine(DestroyBoxColliderAfterTime());
    }

    private IEnumerator DestroyBoxColliderAfterTime()
    {
        yield return new WaitForSeconds(.1f); // Wait a short time
        if (underBridgeIndicators.Count == 0)
        {
            Debug.Log("No PlacementIndicators found under the bridge.");
        }
        else
        {
            Debug.Log($"Found {underBridgeIndicators.Count} PlacementIndicators under the bridge.");
            MakeBridgeTransparent();
        }
        Destroy(boxCollider);
    }

    public void AddIndicator(PlacementIndicator indicator)
    {
        if (indicator != null && !topOfBridgeIndicators.Contains(indicator))
        {
            underBridgeIndicators.Add(indicator);
            Debug.Log($"Added {indicator.name} to under bridge indicators.");
        }
    }

    private void OnTriggerEnter(Collider other)
    {
        PlacementIndicator indicator = other.GetComponent<PlacementIndicator>();
        if (indicator != null && !indicator.transform.IsChildOf(transform) && !underBridgeIndicators.Contains(indicator))
        {
            underBridgeIndicators.Add(indicator);
            Debug.Log($"Added {indicator.name} to underBridgeIndicators.");
        }
    }

    // private void OnTriggerExit(Collider other)
    // {
    //     PlacementIndicator indicator = other.GetComponent<PlacementIndicator>();
    //     if (indicator != null && underBridgeIndicators.Contains(indicator))
    //     {
    //         underBridgeIndicators.Remove(indicator);
    //     }
    // }

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
            childIndicator.gameObject.SetActive(false);
        }
    }

    private void MaterializeBridge()
    {
        bridgeRenderer.material.color = new Color(bridgeRenderer.material.color.r, bridgeRenderer.material.color.g, bridgeRenderer.material.color.b, 1f);
        meshCollider.enabled = true;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            childIndicator.gameObject.SetActive(true);
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
