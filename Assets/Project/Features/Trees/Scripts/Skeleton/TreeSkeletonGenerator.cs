using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Gera o esqueleto (tronco + galhos) de uma espécie a partir de uma seed. Determinístico: mesma
    /// espécie e seed sempre geram a mesma árvore. Reutilizável: mantém os buffers entre chamadas.
    /// </summary>
    public sealed class TreeSkeletonGenerator
    {
        // Limite de segurança contra configurações que explodem em milhares de galhos
        private const int MaxBranches = 256;
        private const int MinSegments = 2;
        private const float MinRadiusMultiplier = 0.05f;
        private const float ParallelThreshold = 0.99f;

        private readonly List<BranchAttachment> _attachments = new List<BranchAttachment>();

        public void Generate(TreeSpeciesDefinition species, int seed, TreeSkeleton skeleton)
        {
            skeleton.Clear();
            uint baseSeed = unchecked((uint)seed);
            TreeMeshSettings mesh = species.Mesh;

            // Cada nível tem sua própria sequência aleatória: mexer nos galhos não muda o formato do tronco
            var trunkRandom = new SeededRandom(SeededRandom.Combine(baseSeed, 0u));
            GrowTrunk(species.Trunk, mesh, skeleton, ref trunkRandom);

            int levelStart = 0;
            int levelEnd = skeleton.BranchCount;
            IReadOnlyList<BranchLevelSettings> levels = species.BranchLevels;
            for (int level = 0; level < levels.Count; level++)
            {
                BranchLevelSettings settings = levels[level];
                if (settings == null || settings.Layout == null) break;

                var levelRandom = new SeededRandom(SeededRandom.Combine(baseSeed, (uint)level + 1u));
                for (int parent = levelStart; parent < levelEnd; parent++)
                {
                    GrowChildren(skeleton, parent, level + 1, settings, mesh, ref levelRandom);
                }

                levelStart = levelEnd;
                levelEnd = skeleton.BranchCount;
                if (levelStart == levelEnd) break;
            }
        }

        private static void GrowTrunk(TrunkSettings trunk, TreeMeshSettings mesh, TreeSkeleton skeleton, ref SeededRandom random)
        {
            float height = trunk.Height.Pick(ref random);
            float baseRadius = trunk.BaseRadius.Pick(ref random);

            // Inclinação inicial em uma direção aleatória
            float leanAzimuth = random.NextAngle() * Mathf.Rad2Deg;
            float leanAngle = random.NextFloat() * trunk.MaxLean;
            Vector3 leanAxis = Quaternion.AngleAxis(leanAzimuth, Vector3.up) * Vector3.right;
            Vector3 direction = Quaternion.AngleAxis(leanAngle, leanAxis) * Vector3.up;

            var shape = new BranchShape(height, baseRadius, trunk.RadiusOverLength, trunk.Curvature, trunk.UpwardPull,
                trunk.RootFlare, trunk.RootFlareHeight);
            skeleton.BeginBranch(0, TreeBranch.NoParent);
            GrowAxis(skeleton, Vector3.zero, direction, shape, mesh.SegmentLength, ref random);
        }

        private void GrowChildren(TreeSkeleton skeleton, int parentIndex, int level, BranchLevelSettings settings,
            TreeMeshSettings mesh, ref SeededRandom random)
        {
            TreeBranch parent = skeleton.GetBranch(parentIndex);
            _attachments.Clear();
            settings.Layout.Layout(parent.Length, ref random, _attachments);

            for (int i = 0; i < _attachments.Count; i++)
            {
                if (skeleton.BranchCount >= MaxBranches) return;

                BranchAttachment attachment = _attachments[i];
                skeleton.Sample(parentIndex, attachment.Position, out Vector3 start, out Vector3 parentDirection, out float parentRadius);

                // Sorteia tudo antes de decidir pular, para um galho pulado não mudar o sorteio dos próximos
                float radius = parentRadius * settings.RadiusRatio.Pick(ref random);
                float length = parent.Length * settings.LengthRatio.Pick(ref random) *
                               settings.LengthOverParent.Evaluate(attachment.Position);
                float angle = settings.Angle.Pick(ref random);
                if (radius < mesh.MinBranchRadius || length < mesh.SegmentLength * 0.5f) continue;

                Vector3 direction = BranchDirection(parentDirection, attachment.Azimuth, angle);
                var shape = new BranchShape(length, radius, settings.RadiusOverLength, settings.Curvature, settings.Gravity, 0f, 0f);
                skeleton.BeginBranch(level, parentIndex);
                GrowAxis(skeleton, start, direction, shape, mesh.SegmentLength, ref random);
            }
        }

        // Caminha ao longo do galho em passos de ~segmentLength, entortando e puxando para cima/baixo
        private static void GrowAxis(TreeSkeleton skeleton, Vector3 start, Vector3 direction, in BranchShape shape,
            float segmentLength, ref SeededRandom random)
        {
            int segments = Mathf.Max(MinSegments, Mathf.CeilToInt(shape.Length / segmentLength));
            float step = shape.Length / segments;
            Vector3 position = start;

            for (int i = 0; i <= segments; i++)
            {
                float distance = i * step;
                float t = (float)i / segments;
                float radius = shape.StartRadius * Mathf.Max(MinRadiusMultiplier, shape.RadiusOverLength.Evaluate(t)) *
                               RootFlareMultiplier(shape, distance);
                skeleton.AddNode(new TreeNode(position, direction, radius, distance));
                if (i == segments) break;

                direction = Wander(direction, shape.Curvature * step, ref random);
                direction = (direction + Vector3.up * (shape.UpwardPull * step)).normalized;
                position += direction * step;
            }
        }

        private static float RootFlareMultiplier(in BranchShape shape, float distance)
        {
            if (shape.RootFlare <= 0f) return 1f;

            // Suave: máximo no pé, zero a partir de RootFlareHeight
            float fade = 1f - Mathf.SmoothStep(0f, 1f, distance / shape.RootFlareHeight);
            return 1f + shape.RootFlare * fade;
        }

        // Gira a direção num ângulo aleatório (até maxDegrees) em volta de um eixo perpendicular aleatório
        private static Vector3 Wander(Vector3 direction, float maxDegrees, ref SeededRandom random)
        {
            if (maxDegrees <= 0f) return direction;

            Vector3 axis = Quaternion.AngleAxis(random.NextAngle() * Mathf.Rad2Deg, direction) * Perpendicular(direction);
            return Quaternion.AngleAxis(random.NextSigned() * maxDegrees, axis) * direction;
        }

        private static Vector3 BranchDirection(Vector3 parentDirection, float azimuth, float angleDegrees)
        {
            Vector3 outward = Quaternion.AngleAxis(azimuth * Mathf.Rad2Deg, parentDirection) * Perpendicular(parentDirection);
            float angle = angleDegrees * Mathf.Deg2Rad;
            return (parentDirection * Mathf.Cos(angle) + outward * Mathf.Sin(angle)).normalized;
        }

        /// <summary>Um vetor unitário perpendicular a <paramref name="direction"/>, estável entre chamadas.</summary>
        public static Vector3 Perpendicular(Vector3 direction)
        {
            Vector3 reference = Mathf.Abs(direction.y) < ParallelThreshold ? Vector3.up : Vector3.right;
            return Vector3.Cross(direction, reference).normalized;
        }

        private readonly struct BranchShape
        {
            public BranchShape(float length, float startRadius, AnimationCurve radiusOverLength, float curvature,
                float upwardPull, float rootFlare, float rootFlareHeight)
            {
                Length = length;
                StartRadius = startRadius;
                RadiusOverLength = radiusOverLength;
                Curvature = curvature;
                UpwardPull = upwardPull;
                RootFlare = rootFlare;
                RootFlareHeight = rootFlareHeight;
            }

            public float Length { get; }
            public float StartRadius { get; }
            public AnimationCurve RadiusOverLength { get; }
            public float Curvature { get; }
            public float UpwardPull { get; }
            public float RootFlare { get; }
            public float RootFlareHeight { get; }
        }
    }
}
