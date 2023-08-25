using System.Collections.Generic;
using UnityEngine;
namespace deloneTriangulation
{
    public class DelaunayTriangulation
    {
        public static List<int> Triangulate(List<Vector2> _verts)
        {
            List<Vector2> verts = new(_verts);
            List<Vector2> points = new(verts);//pop from this list until empty
            #region supertriangle & setup
            if (points.Count < 3) { Debug.LogWarning("no triangles with < 3 points!"); return new() { 0, 1 }; }

            int supertriScale = 50;
            verts.Add(new(-supertriScale, -2 * supertriScale));
            verts.Add(new(supertriScale * 2, -supertriScale));
            verts.Add(new(supertriScale, 2 * supertriScale));

            int Pcount = verts.Count;
            int pointIdx = 0;
            #endregion
            List<int> triangulation = new() { Pcount - 3, Pcount - 2, Pcount - 1 };
            //======================================================================================

            while (points.Count > 0)
            {
                //====POP POINT, INCREMENT I========================================================
                Vector2 newpoint = points[0];
                int idx = pointIdx;
                pointIdx++;
                Debug.Log("introducing point: " + newpoint + " to triangulation" + " at index:" + idx);
                points.RemoveAt(0);
                //==================================================================================


                List<int> bad_triangles;
                //======FOR triangles, check circumcircle. if newpoint in circumcircle, add to list.
                bad_triangles = FindBadTris(newpoint, verts, triangulation);


                //==================================================================================
                List<edge> polygonal_outline;
                //======FOR BAD triangles, find polygonal hole. 
                polygonal_outline = GetConvexHull(bad_triangles);


                //==================================================================================
                //======FOR EDGES IN POLY HOLE .. connect to new_point to re-place triangles, set/return new list
                triangulation = re_triangulate(idx, triangulation, polygonal_outline);

            }
            //========================remove supertriangle from triangulation===============
            return RemoveSuperTriangle(triangulation, Pcount - 3);
        }
        static List<int> FindBadTris(Vector2 newpoint, List<Vector2> verts, List<int> triangulation)
        {
            List<int> bad_triangles = new();

            List<int> removeIdxs = new();

            //======FOR triangles, check circumcircle. if newpoint in circumcircle, add to list.
            for (int n = 0; n < triangulation.Count; n += 3)
            {
                int triA = triangulation[n + 0];
                int triB = triangulation[n + 1];
                int triC = triangulation[n + 2];
                Vector3 A = verts[triA];
                Vector3 B = verts[triB];
                Vector3 C = verts[triC];


                if (PointInCircumcircle(newpoint, A, B, C))
                {
                    List<int> badtri = new() { triA, triB, triC };
                    bad_triangles.AddRange(badtri);

                    Debug.Log("bad triangle found circumscribed: " + triA + "," + triB + "," + triC);
                    Debug.Log("bad triangle indices: " + n + "," + (n + 1) + "," + (n + 2));

                    List<int> IDXs = new() { n, n + 1, n + 2 };
                    removeIdxs.AddRange(IDXs);
                }
            }

            #region remove all bad triangle indices from list, set triangulation equal to new list.
            List<int> newtriangulation = new();
            for (int i = 0; i < triangulation.Count; i += 3)
            {
                #region check if tri is bad/deleted
                bool skip = false;
                for (int j = 0; j < removeIdxs.Count; j++)
                {
                    int removeIdx = removeIdxs[j];
                    if (i == removeIdx){ skip = true; break; }
                }
                #endregion
                if (skip) { continue; }

                newtriangulation.Add(triangulation[i]);
                newtriangulation.Add(triangulation[i + 1]);
                newtriangulation.Add(triangulation[i + 2]);
            }
            triangulation.Clear();
            triangulation.AddRange(newtriangulation);
            #endregion

            return bad_triangles;
        }
        //============================= this method below might need more attention, is generated by Ai
        public static bool PointInCircumcircle(Vector3 Pcheck, Vector3 P0, Vector3 P1, Vector3 P2)
        {
            Vector2 center;
            float radius;

            float dA = P0.sqrMagnitude;
            float dB = P1.sqrMagnitude;
            float dC = P2.sqrMagnitude;

            float aux1 = (dA * (P2.y - P1.y) + dB * (P0.y - P2.y) + dC * (P1.y - P0.y));
            float aux2 = (dA * (P2.x - P1.x) + dB * (P0.x - P2.x) + dC * (P1.x - P0.x));

            float div = 2 * (P0.x * (P2.y - P1.y) + P1.x * (P0.y - P2.y) + P2.x * (P1.y - P0.y));

            if (Mathf.Abs(div) < 0.00001f)
            {
                return false; // Points are nearly collinear, return false
            }

            center = new Vector2(-aux1 / div, -aux2 / div); // Corrected sign here
            radius = Vector2.Distance(center, P0);

            float dx = center.x - Pcheck.x;
            float dy = center.y - Pcheck.y;
            float distanceSquared = dx * dx + dy * dy;

            bool incirc = distanceSquared <= radius * radius;

            Debug.Log("Checking circle at: " + center + " with radius " + radius + ". Inside circle: " + incirc + " for point at: " + Pcheck);

            return incirc;
        }
        struct edge
        {
            //edge struct use to pass data about remaining edges
            //when removing all triangles in circumcircle, preserve the outter boundary edges
            //this forms the "polygonal hole" left by the removal.
            //we save these edges, so we can link them up with the newly-placed vertex. and retriangulate.
            public int idx0;
            public int idx1;
            public int oldIdx;
            public edge(int P0, int P1, int oldidx)
            {
                idx0 = P0;
                idx1 = P1;
                oldIdx = oldidx;
            }
        }
        static List<edge> GetConvexHull(List<int> badTris)
        {
            List<edge> outline = new();
            for (int n = 0; n < badTris.Count; n += 3)
            {
                int A = badTris[n];
                int B = badTris[n + 1];
                int C = badTris[n + 2];
                //triangle has 3 edges
                edge AB = new(A, B, C);
                edge AC = new(A, C, B);
                edge BC = new(B, C, A);
              
                //assume that any of the edges could be added as poly outline
                if (EdgeInOutline(AB, badTris)) { outline.Add(AB); Debug.Log("AB edge saved at " + n); }
                if (EdgeInOutline(AC, badTris)) { outline.Add(AC); Debug.Log("AC edge saved at " + n); }
                if (EdgeInOutline(BC, badTris)) { outline.Add(BC); Debug.Log("BC edge saved at " + n); }
            }

            return outline;
        }
        static bool EdgeInOutline(edge edgeToCheck, List<int> badTriangleIndices)
        {
            HashSet<int> edgeVertices = new()
            {
              edgeToCheck.idx0,
              edgeToCheck.idx1
            };

            for(int i = 0;i<badTriangleIndices.Count;i+=3)
            {

                int sharedVerticesCount = 0;

                int A = badTriangleIndices[i];
                int B = badTriangleIndices[i+1];
                int C = badTriangleIndices[i+2];
                //the three ways that we potentially stored edges along with their old completing verts
                if(A== edgeToCheck.idx0 && B == edgeToCheck.idx1 && C == edgeToCheck.oldIdx) { continue; }
                if (A == edgeToCheck.idx0 && C == edgeToCheck.idx1 && B == edgeToCheck.oldIdx) { continue; }
                if (B == edgeToCheck.idx0 && C == edgeToCheck.idx1 && A == edgeToCheck.oldIdx) { continue; }
                //this checks if we are comparing tri against itself^
                if (edgeVertices.Contains(A)) { sharedVerticesCount++; }
                if (edgeVertices.Contains(B)) { sharedVerticesCount++; }
                if (edgeVertices.Contains(C)) { sharedVerticesCount++; }

                if (sharedVerticesCount == 2)
                {
                    return false; // The edge is shared between bad triangles
                }
            }

            return true; // The edge is not shared between bad triangles
        }
        static List<int> re_triangulate(int newpointIdx, List<int> triangulation, List<edge> polyHole)
        {

            List<int> tris = new(triangulation);
            //for each of the edges
            for (int i = 0; i < polyHole.Count; i++)
            {
                edge PolyEdge = polyHole[i];
                List<int> triangle = new() { PolyEdge.idx0, PolyEdge.idx1, newpointIdx };
                tris.AddRange(triangle);
                Debug.Log("edge:" + PolyEdge.idx0 + "," + PolyEdge.idx1);
            }

            return tris;
        }
        static List<int> RemoveSuperTriangle(List<int> triangulation, int numVertices)
        {
            List<int> newTriangulation = new();

            for (int i = 0; i < triangulation.Count; i += 3)
            {
                int vertexIdx0 = triangulation[i];
                int vertexIdx1 = triangulation[i + 1];
                int vertexIdx2 = triangulation[i + 2];

                // Check if any vertex index belongs to the supertriangle
                if (vertexIdx0 >= numVertices || vertexIdx1 >= numVertices || vertexIdx2 >= numVertices)
                {
                    continue; // Skip triangles involving supertriangle vertices
                }

                newTriangulation.Add(vertexIdx0);
                newTriangulation.Add(vertexIdx1);
                newTriangulation.Add(vertexIdx2);
            }

            return newTriangulation;
        }
    }
}