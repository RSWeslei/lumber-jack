using System;
using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// O pai se divide na ponta em vários galhos grossos (copa de carvalho, árvore em "Y").
    /// Use com ângulo pequeno e raio parecido com o do pai.
    /// </summary>
    [Serializable]
    public sealed class ForkBranchLayout : IBranchLayout
    {
        private const int MaxForks = 6;
        private const float TipPosition = 1f;

        [Tooltip("Em quantos galhos o pai se divide")]
        [SerializeField] private IntRange _forkCount = new(2, 3);
        [Tooltip("Variação da direção de cada divisão (0 = simétrica)")]
        [Range(0f, 1f)]
        [SerializeField] private float _jitter = 0.3f;

        public ForkBranchLayout()
        {
        }

        public ForkBranchLayout(IntRange forkCount, float jitter)
        {
            _forkCount = forkCount;
            _jitter = jitter;
        }

        public void Layout(float parentLength, ref SeededRandom random, List<BranchAttachment> results)
        {
            int count = _forkCount.Pick(ref random);
            if (count <= 0) return;

            float spacing = Mathf.PI * 2f / count;
            float startAzimuth = random.NextAngle();
            for (int i = 0; i < count; i++)
            {
                float azimuth = startAzimuth + i * spacing + random.NextSigned() * _jitter * spacing * 0.5f;
                results.Add(new BranchAttachment(TipPosition, azimuth));
            }
        }

        public void Validate()
        {
            _forkCount = _forkCount.Clamped(0, MaxForks);
        }
    }
}
