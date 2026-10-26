using System.Collections.Generic;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Folhas de uma árvore ou de um pedaço dela: um filho só com malha, sem colisor (o machado mira através
    /// delas). Guarda quais tufos da <see cref="TreeFoliage"/> estão aqui; num corte, os do lado de fora saem.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class TreeLeaves : MonoBehaviour
    {
        private const string ObjectName = "Leaves";
        private const string MeshName = "TreeLeaves";

        // Montagem acontece uma por vez na thread principal: um construtor para todas as folhas
        private static readonly TreeLeavesMeshBuilder SharedBuilder = new();

        private readonly List<int> _clusters = new();
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private Mesh _mesh;
        private Material[] _materials;
        private TreeFoliage _foliage;
        private TreeLeafSettings _settings;
        private bool _hasComponents;
        private bool _hasMesh;
        private bool _hasFoliage;

        /// <summary>Tufos desta parte (índices da folhagem da árvore).</summary>
        public IReadOnlyList<int> Clusters => _clusters;

        public int TriangleCount { get; private set; }

        private void OnDisable()
        {
            // No editor a malha (DontSave) é refeita pela árvore: destruir aqui evita vazar uma por recompilação
            if (!Application.isPlaying) DestroyMesh();
        }

        private void OnDestroy()
        {
            DestroyMesh();
        }

        /// <summary>Cria o filho de folhas de <paramref name="owner"/>. No editor fora do Play ele não é salvo na cena.</summary>
        public static TreeLeaves Create(Transform owner)
        {
            var leavesObject = new GameObject(ObjectName) { layer = owner.gameObject.layer };
            if (!Application.isPlaying) leavesObject.hideFlags = HideFlags.DontSave;
            leavesObject.transform.SetParent(owner, false);
            return leavesObject.AddComponent<TreeLeaves>();
        }

        /// <summary>Mostra todos os tufos da folhagem (árvore recém-gerada).</summary>
        public void ShowAll(TreeFoliage foliage, TreeLeafSettings settings)
        {
            _clusters.Clear();
            for (int i = 0; i < foliage.ClusterCount; i++) _clusters.Add(i);
            Use(foliage, settings);
        }

        /// <summary>Mostra só os tufos <paramref name="clusters"/> (pedaço que saiu de um corte).</summary>
        public void Show(TreeFoliage foliage, TreeLeafSettings settings, IReadOnlyList<int> clusters)
        {
            _clusters.Clear();
            for (int i = 0; i < clusters.Count; i++) _clusters.Add(clusters[i]);
            Use(foliage, settings);
        }

        /// <summary>Tira todos os tufos (árvore sem espécie).</summary>
        public void Hide()
        {
            _clusters.Clear();
            CacheComponents();
            _meshRenderer.enabled = false;
        }

        /// <summary>Corte no galho <paramref name="branch"/>: os tufos que vão com a ponta saem daqui e vão para <paramref name="outer"/>.</summary>
        public void Cut(TreeSkeleton skeleton, int branch, Plane plane, List<int> outer)
        {
            if (!_hasFoliage)
            {
                outer.Clear();
                return;
            }

            _foliage.Split(_clusters, skeleton, branch, plane, outer);
            if (outer.Count > 0) Rebuild();
        }

        private void Use(TreeFoliage foliage, TreeLeafSettings settings)
        {
            _foliage = foliage;
            _settings = settings;
            _hasFoliage = true;
            CacheComponents();
            AssignMaterials();
            Rebuild();
        }

        private void Rebuild()
        {
            EnsureMesh();
            SharedBuilder.Build(_foliage, _clusters, _settings, _mesh);
            TriangleCount = SharedBuilder.TriangleCount;
            _meshRenderer.enabled = _clusters.Count > 0;
        }

        private void AssignMaterials()
        {
            IReadOnlyList<Material> materials = _settings.Materials;
            if (_materials == null || _materials.Length != materials.Count) _materials = new Material[materials.Count];
            for (int i = 0; i < materials.Count; i++) _materials[i] = materials[i];
            _meshRenderer.sharedMaterials = _materials;
        }

        private void CacheComponents()
        {
            if (_hasComponents) return;

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _hasComponents = true;
        }

        private void EnsureMesh()
        {
            if (_hasMesh) return;

            // DontSave: a malha é sempre gerada, nunca gravada na cena
            _mesh = new Mesh { name = MeshName, hideFlags = HideFlags.DontSave };
            _meshFilter.sharedMesh = _mesh;
            _hasMesh = true;
        }

        private void DestroyMesh()
        {
            if (!_hasMesh) return;

            if (Application.isPlaying) Destroy(_mesh);
            else DestroyImmediate(_mesh);
            _mesh = null;
            _hasMesh = false;
        }
    }
}
