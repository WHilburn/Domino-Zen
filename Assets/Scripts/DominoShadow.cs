using UnityEngine;
public class DominoShadow : MonoBehaviour
{
    public GameObject shadowPrefab; // Assign a small quad with blue transparent material
    private GameObject shadowInstance;

    void Update()
    {
        if (shadowInstance != null)
        {
            // Position the shadow directly below the domino
            RaycastHit hit;
            if (Physics.Raycast(transform.position, Vector3.down, out hit, Mathf.Infinity))
            {
                shadowInstance.transform.position = hit.point + Vector3.up * 0.01f; // Slight offset
                shadowInstance.transform.rotation = Quaternion.Euler(90, 0, 0); // Keep it flat
            }
        }
    }

    public void CreateShadow()
    {
        if (shadowInstance == null)
            shadowInstance = Instantiate(shadowPrefab, transform.position, Quaternion.identity);
    }

    public void DestroyShadow()
    {
        if (shadowInstance != null)
            Destroy(shadowInstance);
    }
}
