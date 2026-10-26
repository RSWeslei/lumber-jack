using System;
using System.Collections.Generic;
using LumberJack.Wood;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Espécie de árvore (pinheiro, carvalho...): forma do tronco, níveis de galhos, folhas e resolução da malha.
    /// Cada árvore da cena usa uma espécie + uma seed.
    /// </summary>
    [CreateAssetMenu(fileName = "NewTreeSpecies", menuName = "Lumber Jack/Trees/Tree Species")]
    public class TreeSpeciesDefinition : ScriptableObject
    {
        [SerializeField] private string _displayName;
        [Tooltip("Madeira que o tronco vira ao ser derrubado")]
        [SerializeField] private WoodDefinition _wood;
        [SerializeField] private Material _barkMaterial;
        [Tooltip("Material da face cortada (anéis da madeira)")]
        [SerializeField] private Material _cutFaceMaterial;

        [SerializeField] private TrunkSettings _trunk = new();
        [Tooltip("Cada nível nasce nos galhos do anterior; vazia = só o tronco")]
        [SerializeField] private List<BranchLevelSettings> _branchLevels = new();
        [SerializeField] private TreeMeshSettings _mesh = new();
        [SerializeField] private TreeLeafSettings _leaves = new();
        [SerializeField] private TreeFellingSettings _felling = new();
        [SerializeField] private TreeLifecycleSettings _lifecycle = new();
        [Tooltip("Sons da queda e dos pedaços; vazio = sem som")]
        [SerializeField] private TreeSoundsDefinition _sounds;

        public string DisplayName => _displayName;
        public WoodDefinition Wood => _wood;
        public Material BarkMaterial => _barkMaterial;
        public Material CutFaceMaterial => _cutFaceMaterial;
        public TrunkSettings Trunk => _trunk;
        public IReadOnlyList<BranchLevelSettings> BranchLevels => _branchLevels;
        public TreeMeshSettings Mesh => _mesh;
        public TreeLeafSettings Leaves => _leaves;
        public TreeFellingSettings Felling => _felling;
        public TreeLifecycleSettings Lifecycle => _lifecycle;
        public TreeSoundsDefinition Sounds => _sounds;

#if UNITY_EDITOR
        /// <summary>Só no editor: avisa as árvores da cena para se regenerarem quando a espécie é editada.</summary>
        public event Action Changed;

        private void OnValidate()
        {
            _trunk.Validate();
            _leaves.Validate();
            _felling.Validate();
            _lifecycle.Validate();
            foreach (BranchLevelSettings level in _branchLevels)
            {
                level?.Validate();
            }
            Changed?.Invoke();
        }
#endif
    }
}
