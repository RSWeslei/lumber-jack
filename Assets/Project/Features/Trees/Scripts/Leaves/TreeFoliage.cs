using System.Collections.Generic;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Todos os tufos de folhas de uma árvore gerada. A árvore e os pedaços dela guardam só quais tufos têm;
    /// num corte cada tufo vai inteiro com o lado do galho onde está preso.
    /// </summary>
    public sealed class TreeFoliage
    {
        private readonly List<LeafCluster> _clusters = new();

        public int ClusterCount => _clusters.Count;

        public LeafCluster GetCluster(int index)
        {
            return _clusters[index];
        }

        public void Clear()
        {
            _clusters.Clear();
        }

        public void Add(LeafCluster cluster)
        {
            _clusters.Add(cluster);
        }

        /// <summary>
        /// Separa os tufos pelo corte no galho <paramref name="cutBranch"/>: os que vão com a ponta saem de
        /// <paramref name="clusters"/> e vão para <paramref name="outer"/>.
        /// </summary>
        public void Split(List<int> clusters, TreeSkeleton skeleton, int cutBranch, Plane plane, List<int> outer)
        {
            outer.Clear();
            int kept = 0;
            for (int i = 0; i < clusters.Count; i++)
            {
                int index = clusters[i];
                LeafCluster cluster = _clusters[index];
                if (TreeCutter.IsOnOuterSide(skeleton, cluster.Branch, cluster.Anchor, cutBranch, plane))
                {
                    outer.Add(index);
                    continue;
                }
                clusters[kept] = index;
                kept++;
            }
            clusters.RemoveRange(kept, clusters.Count - kept);
        }
    }
}
