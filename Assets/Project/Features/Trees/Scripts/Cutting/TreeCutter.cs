using System;
using System.Collections.Generic;
using LumberJack.Wood;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Corta uma parte de árvore num galho: o galho atingido é cortado pelo plano; os outros vão inteiros
    /// para um lado pelo esqueleto (filhos que nascem além do corte vão com a ponta). Assim o plano infinito
    /// não pega galhos que só cruzam ele longe dali. Reutilizável: mantém os buffers.
    /// </summary>
    public sealed class TreeCutter
    {
        private const int CutFaceSubmesh = 1;

        private const int OuterSide = 1;
        private const int InnerSide = -1;
        private const int PlaneDecides = 0;
        private const float MinAxisSqrMagnitude = 0.0001f;

        private readonly List<int> _branchSides = new();
        // Delegates criados uma vez (passar o método direto alocaria um delegate novo a cada corte)
        private readonly Func<int, int> _forcedSide;
        private readonly Func<Vector3, Vector2> _cutFaceUV;
        private TreePartMesh _part;
        private Vector3 _cutCenter;
        private Vector3 _cutAxisU;
        private Vector3 _cutAxisV;
        private float _cutRadius;

        public TreeCutter()
        {
            _forcedSide = ForcedSide;
            _cutFaceUV = CutFaceUV;
        }

        /// <summary>
        /// Plano do corte no galho, no ponto do eixo mais perto do ponto mirado, com a normal para a ponta
        /// do galho e inclinado em "tilt" graus no plano da tela.
        /// </summary>
        public static Plane GetCutPlane(TreeSkeleton skeleton, int branch, Vector3 localPoint, Vector3 localViewForward,
            Vector3 localViewUp, float tilt, out Vector3 center, out float radius)
        {
            skeleton.ClosestPoint(branch, localPoint, out center, out Vector3 direction, out radius, out _);
            Vector3 tiltAxis = Vector3.ProjectOnPlane(localViewForward, direction);
            if (tiltAxis.sqrMagnitude < MinAxisSqrMagnitude) tiltAxis = localViewUp;
            Vector3 normal = Quaternion.AngleAxis(tilt, tiltAxis.normalized) * direction;
            return new Plane(normal, center);
        }

        /// <summary>"outer" fica com a ponta do galho (lado da normal), "inner" com o resto.</summary>
        public bool TryCut(Mesh mesh, TreePartMesh part, TreeSkeleton skeleton, int branch, Plane plane, Vector3 center,
            float radius, out Mesh outer, out Mesh inner)
        {
            _part = part;
            _branchSides.Clear();
            for (int b = 0; b < skeleton.BranchCount; b++)
            {
                _branchSides.Add(SideOf(skeleton, b, branch, plane));
            }

            // Anéis em volta do eixo do galho, não do plano: no corte inclinado viram elipses e não saem da textura
            skeleton.ClosestPoint(branch, center, out _, out Vector3 branchDirection, out _, out _);
            _cutCenter = center;
            _cutAxisU = TreeSkeletonGenerator.Perpendicular(branchDirection);
            _cutAxisV = Vector3.Cross(branchDirection, _cutAxisU);
            _cutRadius = Mathf.Max(radius, FarthestCutBark(part, branch, plane));

            bool isCut = MeshSlicer.Slice(mesh, plane, CutFaceSubmesh, _cutFaceUV, _forcedSide, new Vector2(branch, 0f),
                out outer, out inner);
            _part = null;
            return isCut;
        }

        /// <summary>O ponto <paramref name="point"/>, preso no galho <paramref name="branch"/>, vai com a ponta do corte?</summary>
        public static bool IsOnOuterSide(TreeSkeleton skeleton, int branch, Vector3 point, int cutBranch, Plane plane)
        {
            int side = SideOf(skeleton, branch, cutBranch, plane);
            return side == PlaneDecides ? plane.GetDistanceToPoint(point) > 0f : side == OuterSide;
        }

        // Galho cortado: o plano decide. Descendente dele: o lado onde nasce o ramo que sai dele. Resto: fica
        private static int SideOf(TreeSkeleton skeleton, int branch, int cutBranch, Plane plane)
        {
            if (branch == cutBranch) return PlaneDecides;

            int current = branch;
            while (true)
            {
                TreeBranch currentBranch = skeleton.GetBranch(current);
                if (currentBranch.Parent == TreeBranch.NoParent) return InnerSide;
                if (currentBranch.Parent == cutBranch)
                {
                    Vector3 start = skeleton.GetNode(currentBranch.FirstNode).Position;
                    return plane.GetDistanceToPoint(start) > 0f ? OuterSide : InnerSide;
                }
                current = currentBranch.Parent;
            }
        }

        private int ForcedSide(int vertex)
        {
            return _branchSides[_part.BranchOf(vertex)];
        }

        // Casca mais longe do eixo onde o plano passa: no corte bem inclinado a face pega trecho mais grosso que o centro
        private float FarthestCutBark(TreePartMesh part, int branch, Plane plane)
        {
            Vector3[] vertices = part.Vertices;
            int[] triangles = part.TrianglesOf(branch);
            float farthest = 0f;
            for (int i = 0; i < triangles.Length; i += 3)
            {
                Vector3 a = vertices[triangles[i]];
                Vector3 b = vertices[triangles[i + 1]];
                Vector3 c = vertices[triangles[i + 2]];
                bool side = plane.GetDistanceToPoint(a) > 0f;
                if (side == plane.GetDistanceToPoint(b) > 0f && side == plane.GetDistanceToPoint(c) > 0f) continue;

                farthest = Mathf.Max(farthest, DistanceToAxis(a), DistanceToAxis(b), DistanceToAxis(c));
            }
            return farthest;
        }

        private float DistanceToAxis(Vector3 point)
        {
            Vector3 offset = point - _cutCenter;
            float u = Vector3.Dot(offset, _cutAxisU);
            float v = Vector3.Dot(offset, _cutAxisV);
            return Mathf.Sqrt(u * u + v * v);
        }

        // Anéis da madeira pela distância ao eixo do galho: a casca fica na borda da textura em qualquer inclinação
        private Vector2 CutFaceUV(Vector3 localPoint)
        {
            Vector3 offset = localPoint - _cutCenter;
            float scale = 0.5f / _cutRadius;
            return new Vector2(Vector3.Dot(offset, _cutAxisU) * scale + 0.5f, Vector3.Dot(offset, _cutAxisV) * scale + 0.5f);
        }
    }
}
