using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Põe um tufo de folhas na ponta de cada galho de um <see cref="TreeSkeleton"/>, recuado para a ponta terminar
    /// dentro dele (o galho entra no tufo e acaba ali, não atravessa). Mesma espécie + mesma seed = mesmos tufos
    /// em todos os jogadores. Reutilizável e sem estado entre árvores.
    /// </summary>
    public sealed class TreeFoliageGenerator
    {
        private const int TrunkIndex = 0;
        // Sal da seed: as folhas não repetem o sorteio do esqueleto nem o da malha
        private const uint FoliageSeedSalt = 0x1EAFu;
        private const float MinHeadingSqrMagnitude = 0.0001f;
        private const float MinTreeHeight = 0.01f;
        private const float MinDirectionInCluster = 0.0001f;
        // Giro aleatório (graus) de cada tufo em volta da vertical: tufos vizinhos não ficam iguais
        private const float YawJitter = 25f;
        // Onde a ponta do galho fica dentro do tufo: fração do caminho do centro até a borda (folga para as facetas)
        private const float TipDepth = 0.5f;

        public void Generate(TreeSkeleton skeleton, TreeLeafSettings settings, int seed, TreeFoliage foliage)
        {
            foliage.Clear();
            if (settings.ToneCount == 0 || skeleton.BranchCount == 0) return;

            uint foliageSeed = SeededRandom.Combine(unchecked((uint)seed), FoliageSeedSalt);
            var random = new SeededRandom(foliageSeed);
            float height = Mathf.Max(skeleton.Height, MinTreeHeight);

            for (int b = 0; b < skeleton.BranchCount; b++)
            {
                TreeBranch branch = skeleton.GetBranch(b);
                if (branch.Level < settings.MinLevel || branch.NodeCount < 2) continue;

                AddCluster(foliage, skeleton, settings, b, settings.FollowBranch, settings.Stretch, height, foliageSeed,
                    ref random);
            }

            // O tufo do topo fica em pé: não acompanha o tronco
            if (settings.HasTopCluster)
            {
                AddCluster(foliage, skeleton, settings, TrunkIndex, 0f, settings.TopStretch, height, foliageSeed, ref random);
            }
        }

        private static void AddCluster(TreeFoliage foliage, TreeSkeleton skeleton, TreeLeafSettings settings, int branch,
            float followBranch, Vector3 stretch, float height, uint foliageSeed, ref SeededRandom random)
        {
            skeleton.Sample(branch, 1f, out Vector3 tip, out Vector3 direction, out _);

            // Sorteia tudo sempre, na mesma ordem: a sequência não depende de qual caminho o código toma
            float size = settings.Size.Pick(ref random) * settings.SizeOverHeight.Evaluate(Mathf.Clamp01(tip.y / height));
            float yaw = random.NextSigned() * YawJitter;
            float fallbackHeading = random.NextAngle();
            int tone = random.RangeInclusive(0, settings.ToneCount - 1);

            // Frente do tufo = rumo do galho no chão, inclinada o quanto ele acompanha o galho
            var heading = new Vector3(direction.x, 0f, direction.z);
            heading = heading.sqrMagnitude < MinHeadingSqrMagnitude
                ? new Vector3(Mathf.Cos(fallbackHeading), 0f, Mathf.Sin(fallbackHeading))
                : heading.normalized;
            Vector3 forward = Vector3.Slerp(heading, direction, followBranch);
            Quaternion rotation = Quaternion.AngleAxis(yaw, Vector3.up) * Quaternion.LookRotation(forward, Vector3.up);
            Vector3 radii = stretch * size;

            // Recua o centro pelo galho até a ponta ficar a TipDepth da borda, medindo no formato (elipsoide) do tufo
            Vector3 local = Quaternion.Inverse(rotation) * direction;
            var scaled = new Vector3(local.x / radii.x, local.y / radii.y, local.z / radii.z);
            float back = Mathf.Min(TipDepth / Mathf.Max(scaled.magnitude, MinDirectionInCluster), skeleton.GetBranch(branch).Length);
            Vector3 center = tip - direction * back;

            uint clusterSeed = SeededRandom.Combine(foliageSeed, (uint)foliage.ClusterCount);
            foliage.Add(new LeafCluster(branch, tip, center, rotation, radii, tone, clusterSeed));
        }
    }
}
