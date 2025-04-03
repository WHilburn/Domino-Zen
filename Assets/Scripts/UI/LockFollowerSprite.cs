using UnityEngine;
// LockSpriteFollower script
public class LockSpriteFollower : MonoBehaviour
{
    private Camera cameraToFollow;
    private Vector3 worldPosition;
    private float fadeDuration;
    private CanvasGroup canvasGroup;
    private float elapsedTime;

    public void Initialize(Camera camera, Vector3 position, float duration)
    {
        cameraToFollow = camera;
        worldPosition = position;
        fadeDuration = duration;
        canvasGroup = gameObject.AddComponent<CanvasGroup>();
        canvasGroup.alpha = 0f;
    }

    void Update()
    {
        if (cameraToFollow == null) return;

        // Update screen position based on world position
        Vector3 screenPosition = cameraToFollow.WorldToScreenPoint(worldPosition);
        RectTransform rectTransform = GetComponent<RectTransform>();
        if (rectTransform != null)
        {
            rectTransform.position = screenPosition;
        }

        // Handle fade out
        elapsedTime += Time.deltaTime;
        if (elapsedTime >= fadeDuration)
        {
            Destroy(gameObject);
        }
        else
        {
            canvasGroup.alpha = Mathf.Lerp(1f, 0f, elapsedTime / fadeDuration);
        }
    }
}