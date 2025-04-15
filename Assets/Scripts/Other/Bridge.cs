using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using DG.Tweening; // Add this import for DOTween

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
        SetMaterialToTransparent(); // Set the material to transparent mode
        bridgeRenderer.material.DOFade(0.5f, 0.5f); // Smoothly fade to 50% alpha over 0.5 seconds
        meshCollider.enabled = false;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            childIndicator.FadeOut(false);
            childIndicator.currentState = PlacementIndicator.IndicatorState.Disabled;
        }
    }

    private void MaterializeBridge()
    {
        bridgeRenderer.material.DOFade(1f, 0.5f).OnComplete(() => SetMaterialToOpaque()); // Smoothly fade to 100% alpha over 0.5 seconds, then set material to opaque
        meshCollider.enabled = true;

        foreach (var childIndicator in topOfBridgeIndicators)
        {
            childIndicator.FadeIn(false);
            childIndicator.currentState = PlacementIndicator.IndicatorState.Empty;
        }
    }

    private void SetMaterialToTransparent()
    {
        Debug.Log("Setting material to transparent mode");
        bridgeRenderer.material.SetOverrideTag("RenderType", "Transparent");
        bridgeRenderer.material.SetInt("_Surface", 1); // Set to Transparent
        bridgeRenderer.material.SetInt("_Blend", (int)UnityEngine.Rendering.BlendMode.Alpha); // Use Alpha blending mode
        bridgeRenderer.material.SetInt("_BlendSrc", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        bridgeRenderer.material.SetInt("_BlendDst", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        bridgeRenderer.material.SetInt("_ZWrite", 0);
        bridgeRenderer.material.EnableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bridgeRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON"); // Disable premultiply
        bridgeRenderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Transparent;
    }

    private void SetMaterialToOpaque()
    {
        Debug.Log("Setting material to opaque mode");
        bridgeRenderer.material.SetOverrideTag("RenderType", "Opaque");
        bridgeRenderer.material.SetInt("_Surface", 0); // Set to Opaque
        bridgeRenderer.material.SetInt("_Blend", (int)UnityEngine.Rendering.BlendMode.One);
        bridgeRenderer.material.SetInt("_BlendSrc", (int)UnityEngine.Rendering.BlendMode.One);
        bridgeRenderer.material.SetInt("_BlendDst", (int)UnityEngine.Rendering.BlendMode.Zero);
        bridgeRenderer.material.SetInt("_ZWrite", 1);
        bridgeRenderer.material.DisableKeyword("_SURFACE_TYPE_TRANSPARENT");
        bridgeRenderer.material.DisableKeyword("_ALPHAPREMULTIPLY_ON");
        bridgeRenderer.material.renderQueue = (int)UnityEngine.Rendering.RenderQueue.Geometry;
    }
}
