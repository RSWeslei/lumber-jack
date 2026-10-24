using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;
using UnityEngine.Rendering;

namespace LumberJack.Trees
{
    /// <summary>
    /// Transforma um <see cref="TreeSkeleton"/> em malha low poly com sombreamento chapado (cada triângulo
    /// com seus vértices e normal da face). Reutilizável: mantém os buffers entre árvores.
    /// </summary>
    public sealed class TreeMeshBuilder
    {
        /// <summary>Canal de UV com o índice do galho de cada vértice (x), usado para cortar galho a galho.</summary>
        public const int BranchChannel = 1;

        private const int MaxUInt16Vertices = 65535;
        // Altura da ponta cônica de cada galho, em raios
        private const float TipLengthInRadii = 0.6f;
        private const float MinTriangleArea = 1e-10f;
        // Sal da seed da malha: a irregularidade dos anéis não repete o sorteio do esqueleto
        private const uint MeshSeedSalt = 0x5EEDu;

        private readonly List<Vector3> _vertices = new List<Vector3>();
        private readonly List<Vector3> _normals = new List<Vector3>();
        private readonly List<Vector2> _uvs = new List<Vector2>();
        private readonly List<int> _triangles = new List<int>();
        private readonly List<Vector2> _branchIds = new List<Vector2>();
        // Pontos dos anéis do galho sendo construído (NodeCount x lados)
        private readonly List<Vector3> _ringPoints = new List<Vector3>();

        private Vector2 _currentBranchId;

        public int VertexCount => _vertices.Count;
        public int TriangleCount => _triangles.Count / 3;

        public void Build(TreeSkeleton skeleton, TreeMeshSettings settings, int seed, Mesh mesh)
        {
            _vertices.Clear();
            _normals.Clear();
            _uvs.Clear();
            _triangles.Clear();
            _branchIds.Clear();

            var random = new SeededRandom(SeededRandom.Combine(unchecked((uint)seed), MeshSeedSalt));
            for (int i = 0; i < skeleton.BranchCount; i++)
            {
                _currentBranchId = new Vector2(i, 0f);
                AddBranch(skeleton, skeleton.GetBranch(i), settings, ref random);
            }

            Apply(mesh);
        }

        private void AddBranch(TreeSkeleton skeleton, TreeBranch branch, TreeMeshSettings settings, ref SeededRandom random)
        {
            int sides = branch.Level == 0 ? settings.TrunkSides : settings.BranchSides;
            BuildRings(skeleton, branch, sides, settings.Irregularity, ref random);

            for (int ring = 0; ring < branch.NodeCount - 1; ring++)
            {
                AddSegment(skeleton.GetNode(branch.FirstNode + ring), skeleton.GetNode(branch.FirstNode + ring + 1),
                    ring, sides, settings.BarkTiling);
            }

            AddTip(skeleton.GetNode(branch.LastNode), branch.NodeCount - 1, sides, settings.BarkTiling);
            // Só o tronco aparece por baixo; a base dos galhos fica dentro do pai
            if (branch.Level == 0) AddBaseCap(skeleton.GetNode(branch.FirstNode), sides);
        }

        // Um anel de vértices por nó. O referencial é transportado de nó em nó (parallel transport),
        // então o anel acompanha as curvas sem torcer
        private void BuildRings(TreeSkeleton skeleton, TreeBranch branch, int sides, float irregularity, ref SeededRandom random)
        {
            _ringPoints.Clear();
            TreeNode first = skeleton.GetNode(branch.FirstNode);
            Vector3 normal = TreeSkeletonGenerator.Perpendicular(first.Direction);
            Vector3 previousDirection = first.Direction;
            float angleStep = Mathf.PI * 2f / sides;

            for (int n = 0; n < branch.NodeCount; n++)
            {
                TreeNode node = skeleton.GetNode(branch.FirstNode + n);
                normal = Quaternion.FromToRotation(previousDirection, node.Direction) * normal;
                normal = Vector3.ProjectOnPlane(normal, node.Direction).normalized;
                Vector3 binormal = Vector3.Cross(node.Direction, normal);
                previousDirection = node.Direction;

                for (int side = 0; side < sides; side++)
                {
                    float angle = side * angleStep;
                    float radius = node.Radius * (1f + random.NextSigned() * irregularity);
                    _ringPoints.Add(node.Position + (normal * Mathf.Cos(angle) + binormal * Mathf.Sin(angle)) * radius);
                }
            }
        }

        private void AddSegment(TreeNode bottom, TreeNode top, int ring, int sides, float tiling)
        {
            Vector3 axisCenter = (bottom.Position + top.Position) * 0.5f;
            float vBottom = bottom.Distance * tiling;
            float vTop = top.Distance * tiling;
            int bottomStart = ring * sides;
            int topStart = bottomStart + sides;

            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Vector3 bottomA = _ringPoints[bottomStart + side];
                Vector3 bottomB = _ringPoints[bottomStart + next];
                Vector3 topA = _ringPoints[topStart + side];
                Vector3 topB = _ringPoints[topStart + next];
                Vector3 outward = (bottomA + bottomB + topA + topB) * 0.25f - axisCenter;

                float uA = (float)side / sides;
                float uB = (float)(side + 1) / sides;
                AddTriangle(bottomA, topA, topB, new Vector2(uA, vBottom), new Vector2(uA, vTop), new Vector2(uB, vTop), outward);
                AddTriangle(bottomA, topB, bottomB, new Vector2(uA, vBottom), new Vector2(uB, vTop), new Vector2(uB, vBottom), outward);
            }
        }

        // Fecha a ponta com um cone baixo (low poly: lê-se como a ponta cortada/afinada do galho)
        private void AddTip(TreeNode last, int ring, int sides, float tiling)
        {
            Vector3 tip = last.Position + last.Direction * (last.Radius * TipLengthInRadii);
            int start = ring * sides;
            float v = last.Distance * tiling;
            var tipUv = new Vector2(0.5f, v + last.Radius * TipLengthInRadii * tiling);

            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                Vector3 a = _ringPoints[start + side];
                Vector3 b = _ringPoints[start + next];
                Vector3 outward = (a + b) * 0.5f - last.Position + last.Direction * last.Radius;
                AddTriangle(a, tip, b, new Vector2((float)side / sides, v), tipUv, new Vector2((float)(side + 1) / sides, v), outward);
            }
        }

        private void AddBaseCap(TreeNode first, int sides)
        {
            Vector3 down = -first.Direction;
            var centerUv = new Vector2(0.5f, 0.5f);
            for (int side = 0; side < sides; side++)
            {
                int next = (side + 1) % sides;
                AddTriangle(_ringPoints[side], first.Position, _ringPoints[next], centerUv, centerUv, centerUv, down);
            }
        }

        // Cada triângulo com vértices próprios = faces chapadas (estilo low poly)
        private void AddTriangle(Vector3 a, Vector3 b, Vector3 c, Vector2 uvA, Vector2 uvB, Vector2 uvC, Vector3 outward)
        {
            Vector3 normal = Vector3.Cross(b - a, c - a);
            if (normal.sqrMagnitude < MinTriangleArea) return;

            // A Unity desenha a face cujos vértices giram no sentido horário; inverte se ficaria virada para dentro
            if (Vector3.Dot(normal, outward) < 0f)
            {
                (b, c) = (c, b);
                (uvB, uvC) = (uvC, uvB);
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
            _uvs.Add(uvA);
            _uvs.Add(uvB);
            _uvs.Add(uvC);
            _branchIds.Add(_currentBranchId);
            _branchIds.Add(_currentBranchId);
            _branchIds.Add(_currentBranchId);
            _triangles.Add(index);
            _triangles.Add(index + 1);
            _triangles.Add(index + 2);
        }

        private void Apply(Mesh mesh)
        {
            mesh.Clear();
            mesh.indexFormat = _vertices.Count > MaxUInt16Vertices ? IndexFormat.UInt32 : IndexFormat.UInt16;
            // Vértices antes dos triângulos (a Unity valida os índices contra os vértices)
            mesh.SetVertices(_vertices);
            mesh.SetNormals(_normals);
            mesh.SetUVs(0, _uvs);
            mesh.SetUVs(BranchChannel, _branchIds);
            mesh.SetTriangles(_triangles, 0);
            mesh.RecalculateBounds();
        }
    }
}
