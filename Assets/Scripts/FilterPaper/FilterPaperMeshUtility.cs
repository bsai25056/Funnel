using System.Collections.Generic;
using UnityEngine;

namespace Laboratory.FilterPaperSystem
{
    public static class FilterPaperMeshUtility
    {
        public static Mesh CreateFoldedConeMesh(
            int segments = 48,
            float topRadius = 0.66f,
            float bottomRadius = 0.085f,
            float topHeight = 0.96f,
            float bottomHeight = 0.10f)
        {
            segments = Mathf.Max(8, segments);

            var vertices = new List<Vector3>(segments * 4);
            var triangles = new List<int>(segments * 12);

            int outerTop = AddRing(vertices, segments, topRadius, topHeight);
            int outerBottom = AddRing(vertices, segments, bottomRadius, bottomHeight);
            int innerTop = AddRing(vertices, segments, topRadius - 0.012f, topHeight - 0.006f);
            int innerBottom = AddRing(vertices, segments, bottomRadius - 0.008f, bottomHeight + 0.006f);

            AddSurface(triangles, segments, outerTop, outerBottom, false);
            AddSurface(triangles, segments, innerTop, innerBottom, true);
            AddSurface(triangles, segments, outerTop, innerTop, true);
            AddSurface(triangles, segments, outerBottom, innerBottom, false);

            var mesh = new Mesh
            {
                name = "FilterPaperMesh",
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
            int start = vertices.Count;
            for (int i = 0; i < segments; i++)
            {
                float angle = Mathf.PI * 2f * i / segments;
                vertices.Add(new Vector3(
                    Mathf.Cos(angle) * radius,
                    height,
                    Mathf.Sin(angle) * radius));
            }

            return start;
        }

        private static void AddSurface(
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
