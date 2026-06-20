using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.FunnelSystem
{
    /// <summary>
    /// Generates a hollow, open funnel mesh without external modelling packages.
    /// The saved FunnelMesh asset is created from this output.
    /// </summary>
    public static class FunnelMeshUtility
    {
        public static Mesh CreateHollowFunnelMesh(
            int segments = 48,
            float topOuterRadius = 0.8f,
            float topInnerRadius = 0.7f,
            float throatOuterRadius = 0.17f,
            float throatInnerRadius = 0.105f,
            float bowlHeight = 1.05f,
            float stemLength = 1.5f)
        {
            segments = Mathf.Max(8, segments);

            var vertices = new List<Vector3>(segments * 6);
            var triangles = new List<int>(segments * 36);

            int topOuter = AddRing(vertices, segments, topOuterRadius, bowlHeight);
            int topInner = AddRing(vertices, segments, topInnerRadius, bowlHeight);
            int throatOuter = AddRing(vertices, segments, throatOuterRadius, 0f);
            int throatInner = AddRing(vertices, segments, throatInnerRadius, 0f);
            int stemBottomOuter = AddRing(vertices, segments, throatOuterRadius, -stemLength);
            int stemBottomInner = AddRing(vertices, segments, throatInnerRadius, -stemLength);

            AddRingSurface(triangles, segments, topOuter, throatOuter, false);
            AddRingSurface(triangles, segments, throatOuter, stemBottomOuter, false);
            AddRingSurface(triangles, segments, topInner, throatInner, true);
            AddRingSurface(triangles, segments, throatInner, stemBottomInner, true);
            AddRingSurface(triangles, segments, topOuter, topInner, true);
            AddRingSurface(triangles, segments, stemBottomOuter, stemBottomInner, false);

            var mesh = new Mesh
            {
                name = "FunnelMesh",
                vertices = vertices.ToArray(),
                triangles = triangles.ToArray()
            };

            mesh.RecalculateNormals();
            mesh.RecalculateBounds();
            return mesh;
        }

        private static int AddRing(
            List<Vector3> vertices,
            int segments,
            float radius,
            float height)
        {
            int startIndex = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vertices.Add(new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius));
            }

            return startIndex;
        }

        private static void AddRingSurface(
            List<int> triangles,
            int segments,
            int firstRing,
            int secondRing,
            bool reverse)
        {
            for (int i = 0; i < segments; i++)
            {
                int next = (i + 1) % segments;
                int a = firstRing + i;
                int b = firstRing + next;
                int c = secondRing + next;
                int d = secondRing + i;

                if (reverse)
                {
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(b);
                    triangles.Add(a);
                    triangles.Add(d);
                    triangles.Add(c);
                }
                else
                {
                    triangles.Add(a);
                    triangles.Add(b);
                    triangles.Add(c);
                    triangles.Add(a);
                    triangles.Add(c);
                    triangles.Add(d);
                }
            }
        }
    }
}
