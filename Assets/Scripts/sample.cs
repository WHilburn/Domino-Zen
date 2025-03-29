using UnityEngine;

public class SemiCircleSpawner : MonoBehaviour
{
    public int n = 10; // Number of cubes
    public float semicircleAngle = 180f; // Angle of semicircle (1 to 360 degrees)
    public float spacing = 1.5f; // Desired spacing between cubes
    public float startAngleOffset = 0f; // Starting angle of the semicircle

    void Update()
    {
        if (Input.GetKeyDown(KeyCode.Space))
        {
            SpawnCubes();
        }
    }

    void SpawnCubes()
    {
        if (n < 2) return; // Need at least 2 cubes for spacing to make sense

        float angleRad = semicircleAngle * Mathf.Deg2Rad; // Convert angle to radians
        float radius = ((n - 1) * spacing) / angleRad; // Compute radius dynamically

        float startAngle = startAngleOffset; // Start from the offset
        float angleStep = semicircleAngle / (n - 1); // Space cubes evenly

        for (int i = 0; i < n; i++)
        {
            float angle = startAngle + i * angleStep;
            float radian = angle * Mathf.Deg2Rad;

            // Calculate cube position
            Vector3 spawnPos = transform.position + new Vector3(Mathf.Cos(radian), 0, Mathf.Sin(radian)) * radius;

            // Instantiate cube
            GameObject cube = GameObject.CreatePrimitive(PrimitiveType.Cube);
            cube.transform.position = spawnPos;

            // Rotate the cube to face the sphere
            cube.transform.LookAt(transform.position);
        }
    }
}
