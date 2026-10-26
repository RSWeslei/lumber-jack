using System;
using LumberJack.WorldTime;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Ciclo de vida da espécie: quanto a muda leva para virar adulta, quanto ela cresce em cada estação, quanto
    /// tempo o toco leva para apodrecer e a chance de ele rebrotar (carvalho e bétula rebrotam, pinheiro não).
    /// </summary>
    [Serializable]
    public sealed class TreeLifecycleSettings
    {
        private const float MinGrowingDays = 0.1f;
        private const float MinSaplingScale = 0.02f;

        [Tooltip("Dias do jogo crescendo no ritmo cheio até a muda virar adulta")]
        [SerializeField] private float _growingDays = 7f;
        [Tooltip("Tamanho da muda ao nascer (fração da árvore adulta)")]
        [Range(MinSaplingScale, 1f)]
        [SerializeField] private float _saplingScale = 0.05f;
        [Tooltip("Ritmo de crescimento em cada estação (1 = ritmo cheio, 0 = parada)")]
        [SerializeField] private float _springRate = 1f;
        [SerializeField] private float _summerRate = 1f;
        [SerializeField] private float _autumnRate = 0.3f;
        [SerializeField] private float _winterRate;
        [Tooltip("Dias do jogo até o toco apodrecer e sumir")]
        [SerializeField] private float _stumpDecayDays = 7f;
        [Tooltip("Chance de o toco soltar um broto novo ao lado dele")]
        [Range(0f, 1f)]
        [SerializeField] private float _resproutChance;

        public float GrowingDays => _growingDays;
        public float SaplingScale => _saplingScale;
        public float StumpDecayDays => _stumpDecayDays;
        public float ResproutChance => _resproutChance;

        public float RateFor(Season season)
        {
            return season switch
            {
                Season.Spring => _springRate,
                Season.Summer => _summerRate,
                Season.Autumn => _autumnRate,
                _ => _winterRate
            };
        }

        public void Validate()
        {
            _growingDays = Mathf.Max(_growingDays, MinGrowingDays);
            _springRate = Mathf.Max(_springRate, 0f);
            _summerRate = Mathf.Max(_summerRate, 0f);
            _autumnRate = Mathf.Max(_autumnRate, 0f);
            _winterRate = Mathf.Max(_winterRate, 0f);
            _stumpDecayDays = Mathf.Max(_stumpDecayDays, MinGrowingDays);
        }
    }
}
