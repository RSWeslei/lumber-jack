using System;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Resolução da malha low poly da árvore. Menos lados e anéis = menos triângulos.
    /// </summary>
    [Serializable]
    public sealed class TreeMeshSettings
    {
        [Tooltip("Lados do tronco (low poly: 6 a 8)")]
        [Range(3, 16)]
        [SerializeField] private int _trunkSides = 7;
        [Tooltip("Lados dos galhos")]
        [Range(3, 12)]
        [SerializeField] private int _branchSides = 5;
        [Tooltip("Distância (m) entre os anéis de vértices ao longo de tronco e galhos")]
        [Range(0.2f, 3f)]
        [SerializeField] private float _segmentLength = 0.7f;
        [Tooltip("Irregularidade dos anéis (fração do raio), para não parecer um cilindro perfeito")]
        [Range(0f, 0.3f)]
        [SerializeField] private float _irregularity = 0.08f;
        [Tooltip("Galhos mais finos que isto (m) não são gerados")]
        [Range(0.005f, 0.2f)]
        [SerializeField] private float _minBranchRadius = 0.02f;
        [Tooltip("Repetições da textura de casca por metro (para quando houver textura)")]
        [Min(0.01f)]
        [SerializeField] private float _barkTiling = 0.5f;

        public int TrunkSides => _trunkSides;
        public int BranchSides => _branchSides;
        public float SegmentLength => _segmentLength;
        public float Irregularity => _irregularity;
        public float MinBranchRadius => _minBranchRadius;
        public float BarkTiling => _barkTiling;
    }
}
