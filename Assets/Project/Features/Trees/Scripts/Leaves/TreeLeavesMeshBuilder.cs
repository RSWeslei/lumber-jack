using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LumberJack.Trees
{
    /// <summary>
    /// Malha low poly de um conjunto de tufos: cada tufo é um sólido facetado (anéis girados e vértices
    /// irregulares), com sombreamento chapado e um submesh por tom. Reutilizável: mantém os buffers.
    /// </summary>
    public sealed class TreeLeavesMeshBuilder
    {
        private const int MaxUInt16Vertices = 65535;
        private const float MinTriangleArea = 1e-10f;

        private readonly List<Vector3> _vertices = new();
        private readonly List<Vector3> _normals = new();
        // Triângulos de cada tom (submesh)
        private readonly List<List<int>> _toneTriangles = new();
        // Pontos do tufo sendo construído: polo de baixo, anéis, polo de cima
        private readonly List<Vector3> _points = new();
        private readonly List<float> _ringHeights = new();
        private readonly List<float> _ringRadii = new();

        public int TriangleCount { get; private set; }

        public void Build(TreeFoliage foliage, List<int> clusters, TreeLeafSettings settings, Mesh mesh)
        {
            _vertices.Clear();
            _normals.Clear();
            int toneCount = settings.ToneCount;
            while (_toneTriangles.Count < toneCount) _toneTriangles.Add(new List<int>());
            for (int t = 0; t < _toneTriangles.Count; t++) _toneTriangles[t].Clear();
            MeasureRings(settings);

            for (int i = 0; i < clusters.Count; i++)
            {
                LeafCluster cluster = foliage.GetCluster(clusters[i]);
                if (cluster.Tone < toneCount) AddCluster(cluster, settings);
            }

            Apply(mesh, toneCount);
        }

        // Altura (-1 a 1) e raio de cada anel saem do perfil da espécie; iguais em todos os tufos
        private void MeasureRings(TreeLeafSettings settings)
        {
            _ringHeights.Clear();
            _ringRadii.Clear();
            int rings = settings.Rings;
            for (int r = 1; r <= rings; r++)
            {
                float t = (float)r / (rings + 1);
                _ringHeights.Add(t * 2f - 1f);
                _ringRadii.Add(Mathf.Max(0f, settings.Profile.Evaluate(t)));
            }
        }

        private void AddCluster(LeafCluster cluster, TreeLeafSettings settings)
        {
            var random = new SeededRandom(cluster.Seed);
            int sides = settings.Sides;
            int rings = _ringHeights.Count;
            float roughness = settings.Roughness;
            float angleStep = Mathf.PI * 2f / sides;

            _points.Clear();
            _points.Add(Point(cluster, Vector3.down, roughness, ref random));
            for (int r = 0; r < rings; r++)
            {
                // Cada anel girado ao acaso: as faces não ficam alinhadas como num cilindro
                float twist = random.NextFloat() * angleStep;
                for (int side = 0; side < sides; side++)
                {
                    float angle = side * angleStep + twist;
                    var local = new Vector3(Mathf.Cos(angle) * _ringRadii[r], _ringHeights[r], Mathf.Sin(angle) * _ringRadii[r]);
                    _points.Add(Point(cluster, local, roughness, ref random));
                }
            }
            _points.Add(Point(cluster, Vector3.up, roughness, ref random));

            List<int> triangles = _toneTriangles[cluster.Tone];
            int top = _points.Count - 1;
            int lastRing = 1 + (rings - 1) * sides;
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                AddTriangle(triangles, cluster.Center, _points[0], _points[1 + side], _points[1 + next]);
                AddTriangle(triangles, cluster.Center, _points[top], _points[lastRing + next], _points[lastRing + side]);
                for (int r = 0; r < rings - 1; r++)
                {
                    int below = 1 + r * sides;
                    int above = below + sides;
                    AddTriangle(triangles, cluster.Center, _points[below + side], _points[above + side], _points[above + next]);
                    AddTriangle(triangles, cluster.Center, _points[below + side], _points[above + next], _points[below + next]);
                }
            }
        }

        // Ponto do tufo unitário levado para a árvore, empurrado para dentro ou para fora ao acaso
        private static Vector3 Point(LeafCluster cluster, Vector3 local, float roughness, ref SeededRandom random)
        {
            Vector3 rough = local * (1f + random.NextSigned() * roughness);
            return cluster.Center + cluster.Rotation * Vector3.Scale(rough, cluster.Radii);
        }

        // Cada triângulo com vértices próprios = faces chapadas; virado para fora do centro do tufo
        private void AddTriangle(List<int> triangles, Vector3 center, Vector3 a, Vector3 b, Vector3 c)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < MinTriangleArea) return;

            if (Vector3.Dot(normal, (a + b + c) / 3f - center) < 0f)
            {
                (b, c) = (c, b);
                normal = -normal;
            }
            normal.Normalize();

            int index = _vertices.Count;
            _vertices.Add(a);
            _vertices.Add(b);
            _vertices.Add(c);
            _normals.Add(normal);
            _normals.Add(normal);
            _normals.Add(normal);
            triangles.Add(index);
            triangles.Add(index + 1);
            triangles.Add(index + 2);
        }

        private void Apply(Mesh mesh, int toneCount)
        {
            mesh.Clear();
            mesh.indexFormat = _vertices.Count > MaxUInt16Vertices ? IndexFormat.UInt32 : IndexFormat.UInt16;
            // Vértices antes dos triângulos (a Unity valida os índices contra os vértices)
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.subMeshCount = Mathf.Max(1, toneCount);
            for (int t = 0; t < toneCount; t++)
            {
                mesh.SetTriangles(_toneTriangles[t], t, false);
            }
            mesh.RecalculateBounds();
            TriangleCount = _vertices.Count / 3;
        }
    }
}
