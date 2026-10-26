using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Um tufo de folhas preso num galho (espaço local da árvore).
    /// </summary>
    public readonly struct LeafCluster
    {
        public LeafCluster(int branch, Vector3 anchor, Vector3 center, Quaternion rotation, Vector3 radii, int tone, uint seed)
        {
            Branch = branch;
            Anchor = anchor;
            Center = center;
            Rotation = rotation;
            Radii = radii;
            Tone = tone;
            Seed = seed;
        }

        public int Branch { get; }

        /// <summary>Ponto do eixo do galho onde o tufo está preso: num corte, decide de que lado ele fica.</summary>
        public Vector3 Anchor { get; }

        public Vector3 Center { get; }
        public Quaternion Rotation { get; }

        /// <summary>Raios (m) do tufo: x = largura, y = altura, z = ao longo do galho.</summary>
        public Vector3 Radii { get; }

        /// <summary>Índice do material (tom) do tufo.</summary>
        public int Tone { get; }

        /// <summary>Seed da irregularidade da malha do tufo.</summary>
        public uint Seed { get; }
    }
}
