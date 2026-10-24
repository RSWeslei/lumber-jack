using System;
using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Galhos alternados subindo em espiral (ângulo de ouro), como na maioria das árvores de folha larga.
    /// </summary>
    [Serializable]
    public sealed class SpiralBranchLayout : IBranchLayout
    {
        // 137.5 graus: cada galho fica o mais longe possível dos anteriores, sem alinhar
        private const float GoldenAngle = 2.3999632f;
        private const int MaxCount = 32;

        [Tooltip("Quantos galhos nascem em cada galho pai")]
        [SerializeField] private IntRange _count = new IntRange(3, 5);
        [Tooltip("Trecho do pai onde os galhos nascem (0 = base, 1 = ponta)")]
        [RangeLimits(0f, 1f)]
        [SerializeField] private FloatRange _span = new FloatRange(0.4f, 0.95f);
        [Tooltip("Variação da altura de cada galho (0 = espaçamento regular)")]
        [Range(0f, 1f)]
        [SerializeField] private float _jitter = 0.3f;

        public SpiralBranchLayout()
        {
        }

        public SpiralBranchLayout(IntRange count, FloatRange span, float jitter)
        {
            _count = count;
            _span = span;
            _jitter = jitter;
        }

        public void Layout(float parentLength, ref SeededRandom random, List<BranchAttachment> results)
        {
            int count = _count.Pick(ref random);
            if (count <= 0) return;

            float slotSize = 1f / count;
            float azimuth = random.NextAngle();
            for (int i = 0; i < count; i++)
            {
                float slot = (i + 0.5f + random.NextSigned() * 0.5f * _jitter) * slotSize;
                results.Add(new BranchAttachment(_span.Lerp(slot), azimuth));
                azimuth += GoldenAngle;
            }
        }

        public void Validate()
        {
            _count = _count.Clamped(0, MaxCount);
            _span = _span.Clamped(0f, 1f);
        }
    }
}
