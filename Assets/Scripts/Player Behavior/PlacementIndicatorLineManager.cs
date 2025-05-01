using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;

public class PlacementIndicatorLineManager
{
    private List<GameObject> lineRendererSets;
    private int poolSize;
    private GameObject lineRendererParent;
    private Vector3 decalPosition;

    public PlacementIndicatorLineManager(int poolSize, MonoBehaviour coroutineRunner)
    {
        this.poolSize = poolSize;
        lineRendererSets = new List<GameObject>(poolSize);
        coroutineRunner.StartCoroutine(WaitToInitialize());
    }

    private IEnumerator WaitToInitialize()
    {
        while (SceneLoader.asyncLoad != null)
        {
            yield return null;
        }
        InitializeLinePool();
    }

    private void InitializeLinePool()
    {
        lineRendererParent = new GameObject("LineRendererParent");
        Vector3[] bottomCorners = {
            new Vector3(-DominoLike.standardDimensions.x / 2, -DominoLike.standardDimensions.y / 2, -DominoLike.standardDimensions.z / 2),
            new Vector3(DominoLike.standardDimensions.x / 2, -DominoLike.standardDimensions.y / 2, -DominoLike.standardDimensions.z / 2),
            new Vector3(DominoLike.standardDimensions.x / 2, -DominoLike.standardDimensions.y / 2, DominoLike.standardDimensions.z / 2),
            new Vector3(-DominoLike.standardDimensions.x / 2, -DominoLike.standardDimensions.y / 2, DominoLike.standardDimensions.z / 2)
        };
        Vector3[] topCorners = {
            new Vector3(-DominoLike.standardDimensions.x / 2, DominoLike.standardDimensions.y / 2, -DominoLike.standardDimensions.z / 2),
            new Vector3(DominoLike.standardDimensions.x / 2, DominoLike.standardDimensions.y / 2, -DominoLike.standardDimensions.z / 2),
            new Vector3(DominoLike.standardDimensions.x / 2, DominoLike.standardDimensions.y / 2, DominoLike.standardDimensions.z / 2),
            new Vector3(-DominoLike.standardDimensions.x / 2, DominoLike.standardDimensions.y / 2, DominoLike.standardDimensions.z / 2)
        };
        for (int i = 0; i < poolSize; i++)
        {
            GameObject lineRendererSet = new GameObject("Line Renderer Set " + i);
            lineRendererSet.transform.SetParent(lineRendererParent.transform);
            lineRendererSets.Add(lineRendererSet);
            var bottomEdges = new LineRenderer[4];
            var sideEdges = new LineRenderer[4];

            for (int j = 0; j < 4; j++)
            {
                bottomEdges[j] = CreateLineRenderer();
                sideEdges[j] = CreateLineRenderer(isEdge: true);
                bottomEdges[j].transform.SetParent(lineRendererSet.transform);
                sideEdges[j].transform.SetParent(lineRendererSet.transform);
                bottomEdges[j].SetPositions(new[] { bottomCorners[j], bottomCorners[(j + 1) % 4] });
                sideEdges[j].SetPositions(new[] { bottomCorners[j], topCorners[j] });
                Gradient gradient = new Gradient();
                gradient.SetKeys(
                new GradientColorKey[] { new GradientColorKey(Color.white, 0f), new GradientColorKey(Color.white, 1f) },
                new GradientAlphaKey[] { new GradientAlphaKey(1f, 0f), new GradientAlphaKey(0f, 1f) });
                sideEdges[j].colorGradient = gradient;
            }
        }
        Debug.Log($"Initialized line pool with {poolSize} sets of line renderers.");
    }

    private LineRenderer CreateLineRenderer(Transform parent = null, bool isEdge = false)
    {
        GameObject lineObject = new GameObject(isEdge ? "Side Edge" : "Bottom Edge");
        lineObject.transform.SetParent(parent);
        LineRenderer lineRenderer = lineObject.AddComponent<LineRenderer>();
        lineRenderer.startWidth = 0.01f;
        lineRenderer.endWidth = 0.01f;
        lineRenderer.material = new Material(Shader.Find("Sprites/Default"));
        lineRenderer.useWorldSpace = false;
        return lineRenderer;
    }

    private HashSet<PlacementIndicator> FindNearbyPlacementIndicators(float maxDistance)
    {
        HashSet<PlacementIndicator> nearbyIndicators = new HashSet<PlacementIndicator>();
        Collider[] hitColliders = Physics.OverlapSphere(PlacementDecalManager.mouseWorldPosition, maxDistance);
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
        HashSet<PlacementIndicator> indicators = FindNearbyPlacementIndicators(maxDistance);
        this.decalPosition = decalPosition;
        if (indicators == null || indicators.Count == 0 || lineRendererSets == null || lineRendererSets.Count == 0) return;

        List<PlacementIndicator> closestIndicators = indicators
            .Where(indicator => indicator != null && indicator.isActiveAndEnabled)
            .OrderBy(indicator => Vector3.Distance(indicator.transform.position, decalPosition))
            .Take(poolSize)
            .ToList();

        for (int i = 0; i < closestIndicators.Count; i++)
        {
            AssignLinesToIndicator(closestIndicators[i], lineRendererSets[i], maxDistance, alphaCurve);
        }
    }

    public void ResetLinePool()
    {
        foreach (var set in lineRendererSets)
        {
            set.SetActive(false);
        }
    }

    private void AssignLinesToIndicator(PlacementIndicator indicator, GameObject lineSet, float maxDistance, AnimationCurve alphaCurve)
    {
        lineSet.transform.position = indicator.transform.position;
        lineSet.transform.rotation = indicator.transform.rotation;

        float distance = Vector3.Distance(indicator.transform.position, decalPosition);
        float normalizedDistance = Mathf.Clamp01(distance / maxDistance); // Normalize distance to [0, 1]
        float alpha = alphaCurve.Evaluate(normalizedDistance); // Use the curve to determine alpha

        foreach (var lineRenderer in lineSet.GetComponentsInChildren<LineRenderer>())
        {
            Gradient gradient = lineRenderer.colorGradient;
            GradientAlphaKey[] alphaKeys = gradient.alphaKeys;

            if (lineRenderer.gameObject.name == "Bottom Edge")
            {
                // Set alpha for both points
                for (int i = 0; i < alphaKeys.Length; i++)
                {
                    alphaKeys[i].alpha = alpha;
                }
            }
            else
            {
                // Set alpha only for the bottom point (first alpha key)
                if (alphaKeys.Length > 0)
                {
                    alphaKeys[0].alpha = alpha; // Bottom point
                }
            }

            gradient.alphaKeys = alphaKeys;
            lineRenderer.colorGradient = gradient;
        }

        lineSet.SetActive(true);
    }
}