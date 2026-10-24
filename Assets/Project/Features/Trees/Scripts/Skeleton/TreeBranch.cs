namespace LumberJack.Trees
{
    /// <summary>
    /// Um galho (ou o tronco) do esqueleto: um trecho contínuo da lista de nós.
    /// </summary>
    public readonly struct TreeBranch
    {
        public const int NoParent = -1;

        public TreeBranch(int firstNode, int nodeCount, int level, int parent, float length)
        {
            FirstNode = firstNode;
            NodeCount = nodeCount;
            Level = level;
            Parent = parent;
            Length = length;
        }

        public int FirstNode { get; }
        public int NodeCount { get; }

        /// <summary>0 = tronco, 1 = galho do tronco, 2 = galho de galho...</summary>
        public int Level { get; }

        /// <summary>Índice do galho pai, ou <see cref="NoParent"/> no tronco.</summary>
        public int Parent { get; }

        /// <summary>Comprimento (m) ao longo do eixo.</summary>
        public float Length { get; }

        public int LastNode => FirstNode + NodeCount - 1;

        public TreeBranch WithNodeAdded(float length)
        {
            return new TreeBranch(FirstNode, NodeCount + 1, Level, Parent, length);
        }
    }
}
