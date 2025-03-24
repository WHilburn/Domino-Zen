using UnityEngine;
using System.Collections;

public class PlacementIndicator : MonoBehaviour
{
    private Renderer indicatorRenderer;
    private Collider placementCollider;
    public float fadeSpeed = 2f;
    private Coroutine fadeCoroutine;
    private GameObject trackedDomino;
    private bool isFadingOut = false;
    private float maxAlpha = 0.6f;
    public float maxDistance = 0.15f;

    void Start()
    {
        indicatorRenderer = GetComponent<Renderer>();
        placementCollider = GetComponent<Collider>();
        SnapToGround();
    }

    private void OnTriggerEnter(Collider other)
    {
        if (other.CompareTag("DominoTag") && trackedDomino == null)
        {
            trackedDomino = other.gameObject;
            StartCoroutine(MonitorDominoPlacement());
        }
    }

    private void OnTriggerExit(Collider other)
    {
        if (other.gameObject == trackedDomino)
        {
            trackedDomino = null;
            if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
            fadeCoroutine = StartCoroutine(FadeIn());
        }
    }

    private void SnapToGround()
    {
        if (Physics.Raycast(transform.position, Vector3.down, out RaycastHit hitInfo, Mathf.Infinity))
        {
            Collider col = GetComponent<Collider>();
            if (col is BoxCollider box)
            {
                float bottomOffset = box.bounds.extents.y; // Get half the height
                transform.position = hitInfo.point + Vector3.up * bottomOffset;
            }
        }
    }

    private IEnumerator MonitorDominoPlacement()
    {
        Rigidbody dominoRb = trackedDomino?.GetComponent<Rigidbody>();

        if (trackedDomino == null || dominoRb == null)
        {
            trackedDomino = null;
            fadeCoroutine = StartCoroutine(FadeIn());
            yield break; // Exit coroutine if the domino is null
        }

        while (trackedDomino != null)
        {
            // Check if trackedDomino is still valid
            if (trackedDomino == null || dominoRb == null)
            {
                fadeCoroutine = StartCoroutine(FadeIn());
                yield break; // Exit the coroutine safely
            }

            // Wait until the domino is stationary
            while (dominoRb != null && (dominoRb.velocity.magnitude > 0.05f || dominoRb.angularVelocity.magnitude > 0.05f))
            {
                yield return null;
            }

            // Ensure trackedDomino still exists
            if (trackedDomino == null || dominoRb == null)
            {
                fadeCoroutine = StartCoroutine(FadeIn());
                yield break;
            }

            // Check if the domino is properly aligned and mostly overlapping
            bool isAligned = Vector3.Angle(trackedDomino.transform.forward, transform.forward) < 10 &&
                             Vector3.Angle(trackedDomino.transform.up, transform.up) < 10;

            bool isOverlapping = placementCollider.bounds.Intersects(trackedDomino.GetComponent<Collider>().bounds);
            // Check that the domino's x and y position is within .15 units of the placement indicator
            bool isPositioned = Vector3.Distance(new Vector3(trackedDomino.transform.position.x, 0, trackedDomino.transform.position.z),
                new Vector3(transform.position.x, 0, transform.position.z)) < maxDistance;

            if (isAligned && isOverlapping && isPositioned && !isFadingOut)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeOut());
            }
            else if ((!isAligned || !isOverlapping || !isPositioned) && isFadingOut)
            {
                if (fadeCoroutine != null) StopCoroutine(fadeCoroutine);
                fadeCoroutine = StartCoroutine(FadeIn());
            }

            yield return new WaitForSeconds(0.1f);
        }

        // If the trackedDomino is null (e.g., destroyed), fade back in
        fadeCoroutine = StartCoroutine(FadeIn());
    }

    private IEnumerator FadeOut()
    {
        isFadingOut = true;
        Color startColor = indicatorRenderer.material.color;
        float alpha = startColor.a;

        while (alpha > 0)
        {
            alpha -= Time.deltaTime * fadeSpeed;
            indicatorRenderer.material.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }

        indicatorRenderer.enabled = false;
    }

    private IEnumerator FadeIn()
    {
        isFadingOut = false;
        indicatorRenderer.enabled = true;
        Color startColor = indicatorRenderer.material.color;
        float alpha = startColor.a;

        while (alpha < maxAlpha)
        {
            alpha += Time.deltaTime * fadeSpeed;
            indicatorRenderer.material.color = new Color(startColor.r, startColor.g, startColor.b, alpha);
            yield return null;
        }
    }
}
