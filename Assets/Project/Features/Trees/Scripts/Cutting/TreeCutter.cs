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
        public const int CutFaceSubmesh = 1;

        private const int OuterSide = 1;
        private const int InnerSide = -1;
        private const int PlaneDecides = 0;
        private const float MinAxisSqrMagnitude = 0.0001f;

        private readonly List<int> _branchSides = new List<int>();
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

            _cutCenter = center;
            _cutAxisU = TreeSkeletonGenerator.Perpendicular(plane.normal);
            _cutAxisV = Vector3.Cross(plane.normal, _cutAxisU);
            _cutRadius = radius;

            bool isCut = MeshSlicer.Slice(mesh, plane, CutFaceSubmesh, _cutFaceUV, _forcedSide, new Vector2(branch, 0f),
                out outer, out inner);
            _part = null;
            return isCut;
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

        // Anéis da madeira centrados no eixo do galho, ocupando a face do corte
        private Vector2 CutFaceUV(Vector3 localPoint)
        {
            Vector3 offset = localPoint - _cutCenter;
            float scale = 0.5f / _cutRadius;
            return new Vector2(Vector3.Dot(offset, _cutAxisU) * scale + 0.5f, Vector3.Dot(offset, _cutAxisV) * scale + 0.5f);
        }
    }
}
