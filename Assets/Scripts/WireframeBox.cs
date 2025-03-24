using UnityEngine;

public class WireframeBox : MonoBehaviour
{
    private BoxCollider box;
    private Material lineMaterial;

    void Start()
    {
        box = GetComponent<BoxCollider>();

        // Create a simple unlit material for lines
        lineMaterial = new Material(Shader.Find("Hidden/Internal-Colored"));
        lineMaterial.hideFlags = HideFlags.HideAndDontSave;
        lineMaterial.SetInt("_SrcBlend", (int)UnityEngine.Rendering.BlendMode.SrcAlpha);
        lineMaterial.SetInt("_DstBlend", (int)UnityEngine.Rendering.BlendMode.OneMinusSrcAlpha);
        lineMaterial.SetInt("_Cull", (int)UnityEngine.Rendering.CullMode.Off);
        lineMaterial.SetInt("_ZWrite", 0);
    }

    void OnRenderObject()
    {
        if (box == null) return;

        // Set the material for line rendering
        lineMaterial.SetPass(0);
        GL.Begin(GL.LINES);
        GL.Color(Color.green); // Change color as needed

        // Get world-space corners
        Vector3 center = box.transform.position + box.center;
        Vector3 size = box.size * 0.5f;
        Vector3[] corners = new Vector3[8];

        for (int i = 0; i < 8; i++)
        {
            corners[i] = center + new Vector3(
                ((i & 1) == 0 ? -1 : 1) * size.x,
                ((i & 2) == 0 ? -1 : 1) * size.y,
                ((i & 4) == 0 ? -1 : 1) * size.z
            );
        }

        // Draw edges
        DrawLine(corners[0], corners[1]);
        DrawLine(corners[1], corners[3]);
        DrawLine(corners[3], corners[2]);
        DrawLine(corners[2], corners[0]);

        DrawLine(corners[4], corners[5]);
        DrawLine(corners[5], corners[7]);
        DrawLine(corners[7], corners[6]);
        DrawLine(corners[6], corners[4]);

        DrawLine(corners[0], corners[4]);
        DrawLine(corners[1], corners[5]);
        DrawLine(corners[2], corners[6]);
        DrawLine(corners[3], corners[7]);

        GL.End();
    }

    void DrawLine(Vector3 start, Vector3 end)
    {
        GL.Vertex(start);
        GL.Vertex(end);
    }
}
