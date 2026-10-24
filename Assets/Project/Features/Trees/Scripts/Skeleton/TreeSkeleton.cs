using System.Collections.Generic;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Eixos de tronco e galhos de uma árvore gerada (sem malha). Serve de base para a malha, o colisor
    /// e, no futuro, para derrubar e dividir a árvore. Reutilizável: <see cref="Clear"/> mantém a memória.
    /// </summary>
    public sealed class TreeSkeleton
    {
        private readonly List<TreeNode> _nodes = new List<TreeNode>();
        private readonly List<TreeBranch> _branches = new List<TreeBranch>();

        public int BranchCount => _branches.Count;
        public int NodeCount => _nodes.Count;

        /// <summary>Ponto mais alto do eixo (m), em relação ao pé da árvore.</summary>
        public float Height { get; private set; }

        public TreeBranch GetBranch(int index)
        {
            return _branches[index];
        }

        public TreeNode GetNode(int index)
        {
            return _nodes[index];
        }

        public void Clear()
        {
            _nodes.Clear();
            _branches.Clear();
            Height = 0f;
        }

        /// <summary>Começa um galho novo; os próximos <see cref="AddNode"/> entram nele.</summary>
        public int BeginBranch(int level, int parent)
        {
            _branches.Add(new TreeBranch(_nodes.Count, 0, level, parent, 0f));
            return _branches.Count - 1;
        }

        /// <summary>Adiciona um nó ao último galho começado.</summary>
        public void AddNode(TreeNode node)
        {
            int branchIndex = _branches.Count - 1;
            _nodes.Add(node);
            _branches[branchIndex] = _branches[branchIndex].WithNodeAdded(node.Distance);
            Height = Mathf.Max(Height, node.Position.y);
        }

        /// <summary>Interpola posição, direção e raio em um ponto do galho (0 = base, 1 = ponta).</summary>
        public void Sample(int branchIndex, float t, out Vector3 position, out Vector3 direction, out float radius)
        {
            TreeBranch branch = _branches[branchIndex];
            float scaled = Mathf.Clamp01(t) * (branch.NodeCount - 1);
            int segment = Mathf.Min(Mathf.FloorToInt(scaled), branch.NodeCount - 2);
            float blend = scaled - segment;

            TreeNode from = _nodes[branch.FirstNode + segment];
            TreeNode to = _nodes[branch.FirstNode + segment + 1];
            position = Vector3.Lerp(from.Position, to.Position, blend);
            direction = Vector3.Slerp(from.Direction, to.Direction, blend);
            radius = Mathf.Lerp(from.Radius, to.Radius, blend);
        }
    }
}
