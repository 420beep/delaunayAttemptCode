using System.Collections.Generic;
using UnityEngine;
namespace deloneTriangulation
{
    public class DebugDelone : MonoBehaviour
    {
        [SerializeField] bool realtime;
        [SerializeField] List<Vector2> points = new();
        [SerializeField] List<int> triangles = new();
        [ContextMenu("triangulate points")]
        private void _Start()
        {
            triangles = DelaunayTriangulation.Triangulate(points);
        }
        private void Update()
        {
            DrawDebugLines(points, triangles);
            if (realtime) { _Start(); }
        }
        public void DrawDebugLines(List<Vector2> vertices, List<int> triangles)
        {
            #region nullcheck
            if (vertices == null || triangles == null)
            {
                return;
            }
            if (vertices.Count == 0 || triangles.Count == 0)
            {
                return;
            }
            #endregion

            for (int i = 0; i < triangles.Count; i += 3)
            {
                int indexA = triangles[i];
                int indexB = triangles[i + 1];
                int indexC = triangles[i + 2];

                Vector3 vertexA = vertices[indexA];
                Vector3 vertexB = vertices[indexB];
                Vector3 vertexC = vertices[indexC];

                float X = (indexC / triangles.Count) + (indexA +i / triangles.Count);
                float Y = (indexC / triangles.Count) + (indexB +i / triangles.Count);
                float Z = (indexC/triangles.Count)+ (i / triangles.Count);
                Vector3 colV = new(X,Y,Z);
                colV *= 0.5f;
                Color col = new(colV.x, colV.y, colV.z);
                Debug.DrawLine(vertexA, vertexB, col);
                Debug.DrawLine(vertexB, vertexC, col);
                Debug.DrawLine(vertexC, vertexA, col);
            }
        }
    }
}