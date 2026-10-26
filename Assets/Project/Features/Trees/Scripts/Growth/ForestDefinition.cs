using System;
using LumberJack.Core;
using LumberJack.WorldTime;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Como uma floresta se renova: mudas das árvores adultas (chance por árvore e por dia em cada estação),
    /// sementes de fora (vento, pássaros: espécies sorteadas pela lista, mesmo que nenhuma árvore delas tenha
    /// sobrado), rebrota dos tocos, espaço entre árvores e limite de árvores na área.
    /// </summary>
    [CreateAssetMenu(fileName = "NewForest", menuName = "Lumber Jack/Trees/Forest")]
    public class ForestDefinition : ScriptableObject
    {
        [Tooltip("Árvores (adultas, mudas e tocos) que a área comporta")]
        [Min(1)]
        [SerializeField] private int _maxTrees = 30;
        [Tooltip("Distância (m) da árvore-mãe em que a muda pode nascer")]
        [RangeLimits(0.5f, 40f)]
        [SerializeField] private FloatRange _spreadDistance = new(3f, 9f);
        [Tooltip("Espaço livre (m) que a muda precisa em volta de outras árvores e tocos")]
        [Min(0.5f)]
        [SerializeField] private float _minSpacing = 3f;
        [Tooltip("Raio (m) da coluna que precisa estar livre de qualquer outra coisa (pedras, prédios, toras)")]
        [Min(0.1f)]
        [SerializeField] private float _clearance = 0.6f;
        [Tooltip("Altura (m) da coluna livre acima do chão (pega tora deitada, galho baixo, beiral)")]
        [Min(0.5f)]
        [SerializeField] private float _clearanceHeight = 3f;
        [Tooltip("Chão mais inclinado que isto (graus) não recebe muda")]
        [Range(0f, 60f)]
        [SerializeField] private float _maxSlope = 25f;

        [Header("Mudas das árvores adultas (chance por árvore, por dia)")]
        [Range(0f, 1f)]
        [SerializeField] private float _springChance = 0.15f;
        [Range(0f, 1f)]
        [SerializeField] private float _summerChance = 0.08f;
        [Range(0f, 1f)]
        [SerializeField] private float _autumnChance = 0.02f;
        [Range(0f, 1f)]
        [SerializeField] private float _winterChance;

        [Header("Sementes de fora (chance de uma muda por dia, em qualquer lugar livre da área)")]
        [Tooltip("Espécies que chegam de fora, com o peso de cada uma")]
        [SerializeField] private SpeciesWeight[] _species = Array.Empty<SpeciesWeight>();
        [Range(0f, 1f)]
        [SerializeField] private float _springOutsideChance = 0.5f;
        [Range(0f, 1f)]
        [SerializeField] private float _summerOutsideChance = 0.25f;
        [Range(0f, 1f)]
        [SerializeField] private float _autumnOutsideChance = 0.1f;
        [Range(0f, 1f)]
        [SerializeField] private float _winterOutsideChance;

        [Header("Rebrota do toco")]
        [Tooltip("Dias do jogo até o broto aparecer ao lado do toco (chance de rebrotar fica na espécie)")]
        [Min(0f)]
        [SerializeField] private float _resproutDelayDays = 2f;

        public int MaxTrees => _maxTrees;
        public FloatRange SpreadDistance => _spreadDistance;
        public float MinSpacing => _minSpacing;
        public float Clearance => _clearance;
        public float ClearanceHeight => _clearanceHeight;
        public float MaxSlope => _maxSlope;
        public float ResproutDelayDays => _resproutDelayDays;

        private void OnValidate()
        {
            _spreadDistance = _spreadDistance.Clamped(0.5f, 40f);
        }

        public float ChanceFor(Season season)
        {
            return season switch
            {
                Season.Spring => _springChance,
                Season.Summer => _summerChance,
                Season.Autumn => _autumnChance,
                _ => _winterChance
            };
        }

        public float OutsideChanceFor(Season season)
        {
            return season switch
            {
                Season.Spring => _springOutsideChance,
                Season.Summer => _summerOutsideChance,
                Season.Autumn => _autumnOutsideChance,
                _ => _winterOutsideChance
            };
        }

        /// <summary>Sorteia uma espécie pelo peso; null se a lista estiver vazia ou sem peso.</summary>
        public TreeSpeciesDefinition PickSpecies(ref SeededRandom random)
        {
            float total = 0f;
            foreach (SpeciesWeight entry in _species) total += entry.Weight;
            float roll = random.NextFloat() * total;
            foreach (SpeciesWeight entry in _species)
            {
                roll -= entry.Weight;
                if (roll < 0f) return entry.Species;
            }
            return null;
        }

        [Serializable]
        private class SpeciesWeight
        {
            [SerializeField] private TreeSpeciesDefinition _species;
            [Min(0f)]
            [SerializeField] private float _weight = 1f;

            public TreeSpeciesDefinition Species => _species;
            public float Weight => _species != null ? _weight : 0f;
        }
    }
}
