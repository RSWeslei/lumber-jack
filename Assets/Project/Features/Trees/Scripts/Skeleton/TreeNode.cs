using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Um ponto do eixo de um galho (espaço local da árvore).
    /// </summary>
    public readonly struct TreeNode
    {
        public TreeNode(Vector3 position, Vector3 direction, float radius, float distance)
        {
            Position = position;
            Direction = direction;
            Radius = radius;
            Distance = distance;
        }

        public Vector3 Position { get; }

        /// <summary>Direção do galho neste ponto (normalizada).</summary>
        public Vector3 Direction { get; }

        public float Radius { get; }

        /// <summary>Distância (m) desde a base do galho, para a textura correr ao longo dele.</summary>
        public float Distance { get; }
    }
}
