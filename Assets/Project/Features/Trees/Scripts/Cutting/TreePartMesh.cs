using System;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Malha de uma parte de árvore (em pé ou pedaço caído) em cache: vértices, galho de cada vértice e
    /// triângulos separados por galho (o preview desenha só o galho mirado).
    /// </summary>
    public sealed class TreePartMesh
    {
        private int[][] _branchTriangles = Array.Empty<int[]>();
        private int[] _branchCounts = Array.Empty<int>();
        private Vector2[] _branchIds = Array.Empty<Vector2>();

        public Vector3[] Vertices { get; private set; } = Array.Empty<Vector3>();

        public void Load(Mesh mesh, int branchCount)
        {
            Vertices = mesh.vertices;
            _branchIds = mesh.uv2;
            int[] triangles = mesh.triangles;

            if (_branchCounts.Length != branchCount) _branchCounts = new int[branchCount];
            Array.Clear(_branchCounts, 0, branchCount);
            for (int i = 0; i < triangles.Length; i += 3) _branchCounts[BranchOf(triangles[i])] += 3;

            _branchTriangles = new int[branchCount][];
            for (int b = 0; b < branchCount; b++) _branchTriangles[b] = new int[_branchCounts[b]];

            Array.Clear(_branchCounts, 0, branchCount);
            for (int i = 0; i < triangles.Length; i += 3)
            {
                int branch = BranchOf(triangles[i]);
                int[] target = _branchTriangles[branch];
                int index = _branchCounts[branch];
                target[index] = triangles[i];
                target[index + 1] = triangles[i + 1];
                target[index + 2] = triangles[i + 2];
                _branchCounts[branch] = index + 3;
            }
        }

        public int BranchOf(int vertex)
        {
            return Mathf.RoundToInt(_branchIds[vertex].x);
        }

        public bool HasBranch(int branch)
        {
            return branch >= 0 && branch < _branchTriangles.Length && _branchTriangles[branch].Length > 0;
        }

        public int[] TrianglesOf(int branch)
        {
            return HasBranch(branch) ? _branchTriangles[branch] : Array.Empty<int>();
        }

        /// <summary>Galho do vértice mais perto do ponto (espaço local), ou -1 se a malha estiver vazia.</summary>
        public int FindBranch(Vector3 localPoint)
        {
            int nearest = -1;
            float bestSqrDistance = float.MaxValue;
            for (int i = 0; i < Vertices.Length; i++)
            {
                float sqrDistance = (Vertices[i] - localPoint).sqrMagnitude;
                if (sqrDistance >= bestSqrDistance) continue;

                bestSqrDistance = sqrDistance;
                nearest = i;
            }
            return nearest < 0 ? -1 : BranchOf(nearest);
        }
    }
}
