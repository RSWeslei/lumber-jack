using System;
using System.Collections.Generic;
using LumberJack.Wood;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Espécie de árvore (pinheiro, carvalho...): forma do tronco, níveis de galhos e resolução da malha.
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

        [SerializeField] private TrunkSettings _trunk = new TrunkSettings();
        [Tooltip("Cada nível nasce nos galhos do anterior; vazia = só o tronco")]
        [SerializeField] private List<BranchLevelSettings> _branchLevels = new List<BranchLevelSettings>();
        [SerializeField] private TreeMeshSettings _mesh = new TreeMeshSettings();
        [SerializeField] private TreeFellingSettings _felling = new TreeFellingSettings();

        public string DisplayName => _displayName;
        public WoodDefinition Wood => _wood;
        public Material BarkMaterial => _barkMaterial;
        public Material CutFaceMaterial => _cutFaceMaterial;
        public TrunkSettings Trunk => _trunk;
        public IReadOnlyList<BranchLevelSettings> BranchLevels => _branchLevels;
        public TreeMeshSettings Mesh => _mesh;
        public TreeFellingSettings Felling => _felling;

#if UNITY_EDITOR
        /// <summary>Só no editor: avisa as árvores da cena para se regenerarem quando a espécie é editada.</summary>
        public event Action Changed;

        private void OnValidate()
        {
            _trunk.Validate();
            _felling.Validate();
            for (int i = 0; i < _branchLevels.Count; i++)
            {
                _branchLevels[i]?.Validate();
            }
            Changed?.Invoke();
        }
#endif
    }
}
