using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Rendering;

// Corta uma malha em duas usando um plano (em espaco local da malha) e fecha
// o buraco do corte com uma "tampa". Funciona para malhas cuja secao de corte
// e convexa ou em formato de estrela (troncos, tabuas, pedacos ja cortados).
public static class MeshSlicer
{
    private const float WeldDistance = 0.0001f;

    private class MeshBuilder
    {
        public readonly List<Vector3> Vertices = new List<Vector3>();
        public readonly List<Vector3> Normals = new List<Vector3>();
        public readonly List<Vector2> UVs = new List<Vector2>();
        public readonly List<int>[] Submeshes;

        public MeshBuilder(int submeshCount)
        {
            Submeshes = new List<int>[submeshCount];
            for (int i = 0; i < submeshCount; i++) Submeshes[i] = new List<int>();
        }

        public int AddVertex(Vector3 position, Vector3 normal, Vector2 uv)
        {
            Vertices.Add(position);
            Normals.Add(normal);
            UVs.Add(uv);
            return Vertices.Count - 1;
        }

        public void AddTriangle(int submesh, int a, int b, int c)
        {
            Submeshes[submesh].Add(a);
            Submeshes[submesh].Add(b);
            Submeshes[submesh].Add(c);
        }

        public Mesh ToMesh(string name)
        {
            Mesh mesh = new Mesh { name = name };
            if (Vertices.Count > 65535) mesh.indexFormat = IndexFormat.UInt32;
            mesh.SetVertices(Vertices);
            mesh.SetNormals(Normals);
            mesh.SetUVs(0, UVs);
            mesh.subMeshCount = Submeshes.Length;
            for (int i = 0; i < Submeshes.Length; i++) mesh.SetTriangles(Submeshes[i], i);
            mesh.RecalculateBounds();
            return mesh;
        }
    }

    // Um vertice "solto" (com normal e uv) usado durante o corte de um triangulo
    private struct Vertex
    {
        public Vector3 Position;
        public Vector3 Normal;
        public Vector2 UV;

        public static Vertex Lerp(Vertex a, Vertex b, float t)
        {
            return new Vertex
            {
                Position = Vector3.Lerp(a.Position, b.Position, t),
                Normal = Vector3.Lerp(a.Normal, b.Normal, t).normalized,
                UV = Vector2.Lerp(a.UV, b.UV, t)
            };
        }
    }

    /// <summary>
    /// Corta a malha. "positive" fica do lado para onde a normal do plano aponta.
    /// capSubmesh e o submesh (material) usado na tampa do corte e capUV gera o uv dela.
    /// Retorna false se o plano nao atravessa a malha.
    /// </summary>
    public static bool Slice(Mesh mesh, Plane plane, int capSubmesh, Func<Vector3, Vector2> capUV,
        out Mesh positive, out Mesh negative)
    {
        positive = null;
        negative = null;

        Vector3[] vertices = mesh.vertices;
        Vector3[] normals = mesh.normals;
        Vector2[] uvs = mesh.uv;
        bool hasNormals = normals.Length == vertices.Length;
        bool hasUVs = uvs.Length == vertices.Length;

        int submeshCount = Mathf.Max(mesh.subMeshCount, capSubmesh + 1);
        MeshBuilder positiveBuilder = new MeshBuilder(submeshCount);
        MeshBuilder negativeBuilder = new MeshBuilder(submeshCount);
        List<Vector3> cutPoints = new List<Vector3>();

        float[] distances = new float[vertices.Length];
        for (int i = 0; i < vertices.Length; i++) distances[i] = plane.GetDistanceToPoint(vertices[i]);

        Vertex GetVertex(int index) => new Vertex
        {
            Position = vertices[index],
            Normal = hasNormals ? normals[index] : Vector3.up,
            UV = hasUVs ? uvs[index] : Vector2.zero
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

                if (side[0] == side[1] && side[1] == side[2])
                {
                    MeshBuilder builder = side[0] ? positiveBuilder : negativeBuilder;
                    AddTriangle(builder, submesh, GetVertex(tri[0]), GetVertex(tri[1]), GetVertex(tri[2]));
                    continue;
                }

                // Acha o vertice que ficou sozinho de um lado e gira o triangulo
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
        AddCap(positiveBuilder, capSubmesh, outline, center, -plane.normal, capUV);
        AddCap(negativeBuilder, capSubmesh, outline, center, plane.normal, capUV);

        positive = positiveBuilder.ToMesh(mesh.name);
        negative = negativeBuilder.ToMesh(mesh.name);
        return true;
    }

    /// <summary>
    /// Contorno do corte (pontos em ordem, espaco local) sem cortar nada. Usado no preview,
    /// que roda todo frame: recebe os arrays da malha ja em cache e preenche <paramref name="outline"/>
    /// (reaproveitada pelo chamador) sem alocar memoria.
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
    /// Volume da malha fechada (usado para calcular a massa dos pedacos).
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
            builder.AddVertex(a.Position, a.Normal, a.UV),
            builder.AddVertex(b.Position, b.Normal, b.UV),
            builder.AddVertex(c.Position, c.Normal, c.UV));
    }

    private static void AddCap(MeshBuilder builder, int submesh, List<Vector3> outline, Vector3 center,
        Vector3 normal, Func<Vector3, Vector2> capUV)
    {
        int centerIndex = builder.AddVertex(center, normal, capUV(center));
        int first = builder.Vertices.Count;
        foreach (Vector3 point in outline) builder.AddVertex(point, normal, capUV(point));

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

    // Junta pontos repetidos (cada ponto do corte aparece em dois triangulos vizinhos), na propria lista
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

    // Ordena os pontos do contorno pelo angulo em volta do centro, no plano do corte. Devolve o centro.
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
        public static readonly AngleComparer Shared = new AngleComparer();

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
