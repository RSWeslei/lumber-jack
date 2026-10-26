using System;
using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Folhagem de uma espécie: um tufo low poly na ponta de cada galho (quais galhos, tamanho, formato e tons).
    /// </summary>
    [Serializable]
    public sealed class TreeLeafSettings
    {
        private const int MaxLevel = 4;
        private const float MinSize = 0.1f;
        private const float MaxSize = 4f;
        private const float MinStretch = 0.1f;
        private const float MaxStretch = 4f;

        [Tooltip("Tons das folhas (cada tufo sorteia um); vazia = sem folhas")]
        [SerializeField] private List<Material> _materials = new();

        [Header("Onde nascem")]
        [Tooltip("Menor nível de galho com tufo na ponta (0 = tronco, 1 = galhos do tronco...)")]
        [Range(0, MaxLevel)]
        [SerializeField] private int _minLevel = 1;
        [Tooltip("Um tufo a mais no topo do tronco")]
        [SerializeField] private bool _hasTopCluster;

        [Header("Tamanho e direção")]
        [Tooltip("Raio (m) de cada tufo")]
        [RangeLimits(MinSize, MaxSize)]
        [SerializeField] private FloatRange _size = new(0.8f, 1.2f);
        [Tooltip("Multiplica o tamanho pela altura na árvore (0 = pé, 1 = topo)")]
        [SerializeField] private AnimationCurve _sizeOverHeight = AnimationCurve.Constant(0f, 1f, 1f);
        [Tooltip("Proporção do tufo: x = largura, y = altura, z = ao longo do galho")]
        [SerializeField] private Vector3 _stretch = Vector3.one;
        [Tooltip("Proporção do tufo do topo")]
        [SerializeField] private Vector3 _topStretch = new(0.8f, 1.5f, 0.8f);
        [Tooltip("Quanto o tufo acompanha a inclinação do galho (0 = fica nivelado)")]
        [Range(0f, 1f)]
        [SerializeField] private float _followBranch = 0.3f;

        [Header("Malha")]
        [Tooltip("Lados de cada tufo")]
        [Range(3, 10)]
        [SerializeField] private int _sides = 6;
        [Tooltip("Anéis de vértices entre a base e o topo do tufo")]
        [Range(1, 5)]
        [SerializeField] private int _rings = 2;
        [Tooltip("Raio do tufo da base (0) ao topo (1)")]
        [SerializeField] private AnimationCurve _profile = DefaultProfile();
        [Tooltip("Irregularidade dos vértices (fração do raio)")]
        [Range(0f, 0.5f)]
        [SerializeField] private float _roughness = 0.15f;

        public int ToneCount => _materials.Count;
        public IReadOnlyList<Material> Materials => _materials;
        public int MinLevel => _minLevel;
        public bool HasTopCluster => _hasTopCluster;
        public FloatRange Size => _size;
        public AnimationCurve SizeOverHeight => _sizeOverHeight;
        public Vector3 Stretch => _stretch;
        public Vector3 TopStretch => _topStretch;
        public float FollowBranch => _followBranch;
        public int Sides => _sides;
        public int Rings => _rings;
        public AnimationCurve Profile => _profile;
        public float Roughness => _roughness;

        public void Validate()
        {
            _size = _size.Clamped(MinSize, MaxSize);
            _stretch = ClampStretch(_stretch);
            _topStretch = ClampStretch(_topStretch);
            if (_sizeOverHeight == null || _sizeOverHeight.length == 0) _sizeOverHeight = AnimationCurve.Constant(0f, 1f, 1f);
            if (_profile == null || _profile.length == 0) _profile = DefaultProfile();
        }

        // Bola: fino nas pontas e mais largo no meio
        private static AnimationCurve DefaultProfile()
        {
            return new AnimationCurve(new Keyframe(0f, 0f, 3f, 3f), new Keyframe(0.5f, 1f), new Keyframe(1f, 0f, -3f, -3f));
        }

        private static Vector3 ClampStretch(Vector3 stretch)
        {
            return new Vector3(Mathf.Clamp(stretch.x, MinStretch, MaxStretch), Mathf.Clamp(stretch.y, MinStretch, MaxStretch),
                Mathf.Clamp(stretch.z, MinStretch, MaxStretch));
        }
    }
}
