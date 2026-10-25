using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

namespace LumberJack.Wood
{
    /// <summary>
    /// Corta uma malha em duas usando um plano (em espaço local da malha) e fecha o buraco do corte
    /// com uma "tampa". Funciona para malhas cuja seção de corte é convexa ou em formato de estrela
    /// (troncos, tábuas, pedaços já cortados).
    /// </summary>
    public static class MeshSlicer
    {
        private const float WeldDistance = 0.0001f;

        private class MeshBuilder
        {
            public readonly List<Vector3> Vertices = new();
            private readonly List<Vector3> _normals = new();
            private readonly List<Vector2> _uvs = new();
            private readonly List<Vector2> _uv2s = new();
            private readonly List<int>[] _submeshes;
            private readonly bool _hasUV2;

            public MeshBuilder(int submeshCount, bool hasUV2)
            {
                _hasUV2 = hasUV2;
                _submeshes = new List<int>[submeshCount];
                for (int i = 0; i < submeshCount; i++) _submeshes[i] = new List<int>();
            }

            public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv, Vector2 uv2)
            {
                Vertices.Add(position);
                _normals.Add(normal);
                _uvs.Add(uv);
                _uv2s.Add(uv2);
                return Vertices.Count - 1;
            }

            public void AddTriangle(int submesh, int a, int b, int c)
            {
                _submeshes[submesh].Add(a);
                _submeshes[submesh].Add(b);
                _submeshes[submesh].Add(c);
            }

            public Mesh ToMesh(string name)
            {
                Mesh mesh = new() { name = name };
                if (Vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
                mesh.SetVertices(Vertices);
                mesh.SetNormals(_normals);
                mesh.SetUVs(0, _uvs);
                if (_hasUV2) mesh.SetUVs(1, _uv2s);
                mesh.subMeshCount = _submeshes.Length;
                for (int i = 0; i < _submeshes.Length; i++) mesh.SetTriangles(_submeshes[i], i);
                mesh.RecalculateBounds();
                return mesh;
            }
        }

        // Um vértice "solto" (com normal e uv) usado durante o corte de um triângulo
        private struct Vertex
        {
            public Vector3 Position;
            public Vector3 Normal;
            public Vector2 UV;
            public Vector2 UV2;

            public static Vertex Lerp(Vertex a, Vertex b, float t)
            {
                return new Vertex
                {
                    Position = Vector3.Lerp(a.Position, b.Position, t),
                    Normal = Vector3.Lerp(a.Normal, b.Normal, t).normalized,
                    UV = Vector2.Lerp(a.UV, b.UV, t),
                    UV2 = Vector2.Lerp(a.UV2, b.UV2, t)
                };
            }
        }

        /// <summary>
        /// Corta a malha. "positive" fica do lado para onde a normal do plano aponta.
        /// capSubmesh é o submesh (material) usado na tampa do corte e capUV gera o uv dela.
        /// Retorna false se o plano não atravessa a malha.
        /// </summary>
        public static bool Slice(Mesh mesh, Plane plane, int capSubmesh, Func<Vector3, Vector2> capUV,
            out Mesh positive, out Mesh negative)
        {
            return Slice(mesh, plane, capSubmesh, capUV, null, Vector2.zero, out positive, out negative);
        }

        /// <summary>
        /// Igual ao outro, mas <paramref name="forcedSide"/> (índice do 1º vértice do triângulo) pode mandar o
        /// triângulo inteiro para um lado: &gt; 0 positivo, &lt; 0 negativo, 0 = o plano decide. O segundo canal
        /// de UV é preservado; a tampa recebe <paramref name="capUV2"/>.
        /// </summary>
        public static bool Slice(Mesh mesh, Plane plane, int capSubmesh, Func<Vector3, Vector2> capUV,
            Func<int, int> forcedSide, Vector2 capUV2, out Mesh positive, out Mesh negative)
        {
            positive = null;
            negative = null;

            Vector3[] vertices = mesh.vertices;
            Vector3[] normals = mesh.normals;
            Vector2[] uvs = mesh.uv;
            Vector2[] uv2s = mesh.uv2;
            bool hasNormals = normals.Length == vertices.Length;
            bool hasUVs = uvs.Length == vertices.Length;
            bool hasUV2s = uv2s.Length == vertices.Length;

            int submeshCount = Mathf.Max(mesh.subMeshCount, capSubmesh + 1);
            MeshBuilder positiveBuilder = new(submeshCount, hasUV2s);
            MeshBuilder negativeBuilder = new(submeshCount, hasUV2s);
            List<Vector3> cutPoints = new();

            float[] distances = new float[vertices.Length];
            for (int i = 0; i < vertices.Length; i++) distances[i] = plane.GetDistanceToPoint(vertices[i]);

            Vertex GetVertex(int index) => new() 
            {
                Position = vertices[index],
                Normal = hasNormals ? normals[index] : Vector3.up,
                UV = hasUVs ? uvs[index] : Vector2.zero,
                UV2 = hasUV2s ? uv2s[index] : Vector2.zero
            };

            for (int submesh = 0; submesh < mesh.subMeshCount; submesh++)
            {
                int[] triangles = mesh.GetTriangles(submesh);
                for (int i = 0; i < triangles.Length; i += 3)
                {
                    int[] tri = { triangles[i], triangles[i + 1], triangles[i + 2] };
                    bool[] side =
                    {
                        distances[tri[0]] >= 0f,
                        distances[tri[1]] >= 0f,
                        distances[tri[2]] >= 0f
                    };

                    int forced = forcedSide == null ? 0 : forcedSide(tri[0]);
                    if (forced != 0) side[0] = side[1] = side[2] = forced > 0;

                    if (side[0] == side[1] && side[1] == side[2])
                    {
                        MeshBuilder builder = side[0] ? positiveBuilder : negativeBuilder;
                        AddTriangle(builder, submesh, GetVertex(tri[0]), GetVertex(tri[1]), GetVertex(tri[2]));
                        continue;
                    }

                    // Acha o vértice que ficou sozinho de um lado e gira o triângulo
                    // para ele ficar em "a", mantendo a ordem (winding) original.
                    int lone = side[0] != side[1] && side[0] != side[2] ? 0 : side[1] != side[0] && side[1] != side[2] ? 1 : 2;
                    int ia = tri[lone], ib = tri[(lone + 1) % 3], ic = tri[(lone + 2) % 3];

                    Vertex a = GetVertex(ia), b = GetVertex(ib), c = GetVertex(ic);
                    Vertex ab = Vertex.Lerp(a, b, distances[ia] / (distances[ia] - distances[ib]));
                    Vertex ca = Vertex.Lerp(a, c, distances[ia] / (distances[ia] - distances[ic]));

                    MeshBuilder loneSide = side[lone] ? positiveBuilder : negativeBuilder;
                    MeshBuilder otherSide = side[lone] ? negativeBuilder : positiveBuilder;

                    AddTriangle(loneSide, submesh, a, ab, ca);
                    AddTriangle(otherSide, submesh, ab, b, c);
                    AddTriangle(otherSide, submesh, ab, c, ca);

                    cutPoints.Add(ab.Position);
                    cutPoints.Add(ca.Position);
                }
            }

            if (positiveBuilder.Vertices.Count == 0 || negativeBuilder.Vertices.Count == 0 || cutPoints.Count < 3)
            {
                return false;
            }

            WeldInPlace(cutPoints);
            Vector3 center = SortAroundCenter(cutPoints, plane.normal);
            List<Vector3> outline = cutPoints;
            if (outline.Count < 3) return false;

            // A tampa do lado positivo olha para o lado negativo e vice-versa
            AddCap(positiveBuilder, capSubmesh, outline, center, -plane.normal, capUV, capUV2);
            AddCap(negativeBuilder, capSubmesh, outline, center, plane.normal, capUV, capUV2);

            positive = positiveBuilder.ToMesh(mesh.name);
            negative = negativeBuilder.ToMesh(mesh.name);
            return true;
        }

        /// <summary>
        /// Contorno do corte (pontos em ordem, espaço local) sem cortar nada. Usado no preview,
        /// que roda todo frame: recebe os arrays da malha já em cache e preenche <paramref name="outline"/>
        /// (reaproveitada pelo chamador) sem alocar memória.
        /// </summary>
        public static void GetCrossSection(Vector3[] vertices, int[] triangles, Plane plane, List<Vector3> outline)
        {
            outline.Clear();
            for (int i = 0; i < triangles.Length; i += 3)
            {
                for (int edge = 0; edge < 3; edge++)
                {
                    Vector3 p0 = vertices[triangles[i + edge]];
                    Vector3 p1 = vertices[triangles[i + (edge + 1) % 3]];
                    float d0 = plane.GetDistanceToPoint(p0);
                    float d1 = plane.GetDistanceToPoint(p1);
                    if ((d0 >= 0f) != (d1 >= 0f))
                    {
                        outline.Add(Vector3.Lerp(p0, p1, d0 / (d0 - d1)));
                    }
                }
            }

            WeldInPlace(outline);
            SortAroundCenter(outline, plane.normal);
        }

        /// <summary>
        /// Volume da malha fechada (usado para calcular a massa dos pedaços).
        /// </summary>
        public static float GetVolume(Mesh mesh)
        {
            Vector3[] vertices = mesh.vertices;
            int[] triangles = mesh.triangles;
            float volume = 0f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                volume += Vector3.Dot(a, Vector3.Cross(b, c)) / 6f;
            }
            return Mathf.Abs(volume);
        }

        private static void AddTriangle(MeshBuilder builder, int submesh, Vertex a, Vertex b, Vertex c)
        {
            builder.AddTriangle(submesh,
                builder.AddVertex(a.Position, a.Normal, a.UV, a.UV2),
                builder.AddVertex(b.Position, b.Normal, b.UV, b.UV2),
                builder.AddVertex(c.Position, c.Normal, c.UV, c.UV2));
        }

        private static void AddCap(MeshBuilder builder, int submesh, List<Vector3> outline, Vector3 center,
            Vector3 normal, Func<Vector3, Vector2> capUV, Vector2 capUV2)
        {
            int centerIndex = builder.AddVertex(center, normal, capUV(center), capUV2);
            int first = builder.Vertices.Count;
            foreach (Vector3 point in outline) builder.AddVertex(point, normal, capUV(point), capUV2);

            for (int i = 0; i < outline.Count; i++)
            {
                int current = first + i;
                int next = first + (i + 1) % outline.Count;

                // No Unity a face da frente segue cross(b - a, c - a)
                Vector3 faceNormal = Vector3.Cross(outline[i] - center, outline[(i + 1) % outline.Count] - center);
                if (Vector3.Dot(faceNormal, normal) >= 0f) builder.AddTriangle(submesh, centerIndex, current, next);
                else builder.AddTriangle(submesh, centerIndex, next, current);
            }
        }

        // Junta pontos repetidos (cada ponto do corte aparece em dois triângulos vizinhos), na própria lista
        private static void WeldInPlace(List<Vector3> points)
        {
            float sqrDistance = WeldDistance * WeldDistance;
            int count = 0;
            for (int i = 0; i < points.Count; i++)
            {
                Vector3 point = points[i];
                bool duplicate = false;
                for (int j = 0; j < count; j++)
                {
                    if ((points[j] - point).sqrMagnitude < sqrDistance)
                    {
                        duplicate = true;
                        break;
                    }
                }
                if (!duplicate) points[count++] = point;
            }
            points.RemoveRange(count, points.Count - count);
        }

        // Ordena os pontos do contorno pelo ângulo em volta do centro, no plano do corte. Devolve o centro.
        private static Vector3 SortAroundCenter(List<Vector3> points, Vector3 planeNormal)
        {
            if (points.Count == 0) return Vector3.zero;

            Vector3 center = Vector3.zero;
            foreach (Vector3 point in points) center += point;
            center /= points.Count;

            Vector3 axisU = Vector3.Cross(planeNormal, Mathf.Abs(planeNormal.y) < 0.9f ? Vector3.up : Vector3.right).normalized;
            AngleComparer.Shared.Set(center, axisU, Vector3.Cross(planeNormal, axisU));
            points.Sort(AngleComparer.Shared);
            return center;
        }

        // Comparador reaproveitado: uma lambda no Sort criaria uma closure nova a cada chamada
        private sealed class AngleComparer : IComparer<Vector3>
        {
            public static readonly AngleComparer Shared = new();

            private Vector3 _origin;
            private Vector3 _axisU;
            private Vector3 _axisV;

            public void Set(Vector3 origin, Vector3 axisU, Vector3 axisV)
            {
                _origin = origin;
                _axisU = axisU;
                _axisV = axisV;
            }

            public int Compare(Vector3 p, Vector3 q)
            {
                return Angle(p).CompareTo(Angle(q));
            }

            private float Angle(Vector3 point)
            {
                Vector3 offset = point - _origin;
                return Mathf.Atan2(Vector3.Dot(offset, _axisV), Vector3.Dot(offset, _axisU));
            }
        }
    }
}
