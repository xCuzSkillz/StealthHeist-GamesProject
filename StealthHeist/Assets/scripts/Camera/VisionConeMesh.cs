using UnityEngine;
using System.Collections.Generic;

[RequireComponent(typeof(MeshFilter))]
public class VisionConeMesh : MonoBehaviour
{
    [Header("View Settings")]
    public float viewAngle = 90f;
    public float viewDistance = 10f;

    [Header("Mesh Settings")]
    public int rayCount = 50;

    [Header("Layers")]
    public LayerMask obstacleMask;

    private Mesh mesh;

    void Start()
    {
        mesh = new Mesh();
        GetComponent<MeshFilter>().mesh = mesh;
    }

    void LateUpdate()
    {
        DrawVisionCone();
    }

    void DrawVisionCone()
    {
        List<Vector3> vertices = new List<Vector3>();
        List<int> triangles = new List<int>();

        vertices.Add(Vector3.zero);

        float angleStep = viewAngle / rayCount;
        float startAngle = -viewAngle / 2f;

        for (int i = 0; i <= rayCount; i++)
        {
            float currentAngle = startAngle + angleStep * i;

            Vector3 direction =
                Quaternion.Euler(0, currentAngle, 0) * transform.forward;

            Vector3 vertex;

            RaycastHit hit;

            if (Physics.Raycast(
                transform.position,
                direction,
                out hit,
                viewDistance,
                obstacleMask,
                QueryTriggerInteraction.Ignore))
            {
                vertex = transform.InverseTransformPoint(hit.point);
            }
            else
            {
                vertex = transform.InverseTransformPoint(
                    transform.position + direction * viewDistance);
            }

            vertices.Add(vertex);
        }

        for (int i = 1; i < vertices.Count - 1; i++)
        {
            triangles.Add(0);
            triangles.Add(i);
            triangles.Add(i + 1);
        }

        mesh.Clear();

        mesh.vertices = vertices.ToArray();
        mesh.triangles = triangles.ToArray();

        mesh.RecalculateNormals();
        mesh.RecalculateBounds();
    }
}