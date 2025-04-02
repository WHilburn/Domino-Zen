using UnityEngine;

public class BlueprintGridDrawer : MonoBehaviour
{
    public Material gridMaterial; // Assign a simple unlit material
    public Color gridColor = Color.white; // White lines on blue background
    public float gridSpacing = 1f; // Space between grid lines
    public int gridSize = 50; // Number of lines in each direction

    private void OnPostRender()
    {
        if (!gridMaterial) return;
        
        gridMaterial.SetPass(0);
        GL.PushMatrix();
        GL.Begin(GL.LINES);
        GL.Color(gridColor);

        // Draw vertical lines
        for (int i = -gridSize; i <= gridSize; i++)
        {
            float x = i * gridSpacing;
            GL.Vertex3(x, -gridSize * gridSpacing, 0);
            GL.Vertex3(x, gridSize * gridSpacing, 0);
        }

        // Draw horizontal lines
        for (int i = -gridSize; i <= gridSize; i++)
        {
            float y = i * gridSpacing;
            GL.Vertex3(-gridSize * gridSpacing, y, 0);
            GL.Vertex3(gridSize * gridSpacing, y, 0);
        }

        GL.End();
        GL.PopMatrix();
    }
}
