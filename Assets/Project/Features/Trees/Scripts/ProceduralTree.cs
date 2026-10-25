using System;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LumberJack.Trees
{
    /// <summary>
    /// Árvore gerada a partir de uma espécie + seed. Gera a malha e o colisor ao ativar, também no editor
    /// (preview), e se regenera quando a espécie ou a seed mudam no inspector.
    /// </summary>
    [ExecuteAlways]
    [DisallowMultipleComponent]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer), typeof(MeshCollider))]
    public class ProceduralTree : MonoBehaviour
    {
        private const string MeshName = "ProceduralTree";

        [SerializeField] private TreeSpeciesDefinition _species;
        [Tooltip("Mesma espécie + mesma seed = mesma árvore em todos os jogadores")]
        [SerializeField] private int _seed = 1;

        // Geradores compartilhados: as árvores são geradas uma por vez na thread principal, então
        // reaproveitar os buffers evita lixo para o GC a cada árvore criada
        private static readonly TreeSkeletonGenerator SharedSkeletonGenerator = new();
        private static readonly TreeMeshBuilder SharedMeshBuilder = new();

        private readonly TreeSkeleton _skeleton = new();
        private MeshFilter _meshFilter;
        private MeshRenderer _meshRenderer;
        private MeshCollider _meshCollider;
        private bool _hasMesh;
        private bool _hasComponents;
        // Atualizado quando a espécie muda (OnEnable, Configure, inspector): Generate roda a partir do
        // inspector e não pode pagar a comparação com null do Unity a cada chamada
        private bool _hasSpecies;

#if UNITY_EDITOR
        private TreeSpeciesDefinition _subscribedSpecies;
        private bool _hasSubscribedSpecies;
#endif

        public TreeSpeciesDefinition Species => _species;
        public TreeSkeleton Skeleton => _skeleton;

        /// <summary>Malha gerada (a árvore inteira, antes de qualquer corte).</summary>
        public Mesh Mesh { get; private set; }
        public bool IsGenerated { get; private set; }
        public int VertexCount { get; private set; }
        public int TriangleCount { get; private set; }

        /// <summary>Disparado depois de cada geração (malha e colisor prontos).</summary>
        public event Action Generated;

        private void Awake()
        {
            CacheComponents();
        }

        private void OnEnable()
        {
            CacheComponents();
            _hasSpecies = _species != null;
            Generate();
#if UNITY_EDITOR
            SubscribeToSpecies();
#endif
        }

        private void OnDisable()
        {
#if UNITY_EDITOR
            UnsubscribeFromSpecies();
#endif
            // No editor a malha (DontSave) é recriada no OnEnable: destruir aqui evita vazar uma por recompilação
            if (!Application.isPlaying) DestroyMesh();
        }

        private void OnDestroy()
        {
            DestroyMesh();
        }

        /// <summary>Troca espécie e seed e regenera (para spawners e para replicar árvores na rede).</summary>
        public void Configure(TreeSpeciesDefinition species, int seed)
        {
#if UNITY_EDITOR
            UnsubscribeFromSpecies();
#endif
            _species = species;
            _seed = seed;
            _hasSpecies = _species != null;
#if UNITY_EDITOR
            SubscribeToSpecies();
#endif
            Generate();
        }

        public void Generate()
        {
            if (!_hasSpecies)
            {
                Clear();
                return;
            }

            EnsureMesh();
            SharedSkeletonGenerator.Generate(_species, _seed, _skeleton);
            SharedMeshBuilder.Build(_skeleton, _species.Mesh, _seed, Mesh);
            VertexCount = SharedMeshBuilder.VertexCount;
            TriangleCount = SharedMeshBuilder.TriangleCount;

            _meshRenderer.sharedMaterial = _species.BarkMaterial;
            // Malha gerada não tem colisão pré-calculada: calcula aqui (e reatribui para o PhysX usar a nova)
            Physics.BakeMesh(Mesh.GetEntityId(), false, _meshCollider.cookingOptions);
            _meshCollider.sharedMesh = null;
            _meshCollider.sharedMesh = Mesh;

            IsGenerated = true;
            Generated?.Invoke();
        }

        private void CacheComponents()
        {
            if (_hasComponents) return;

            _meshFilter = GetComponent<MeshFilter>();
            _meshRenderer = GetComponent<MeshRenderer>();
            _meshCollider = GetComponent<MeshCollider>();
            _hasComponents = true;
        }

        private void EnsureMesh()
        {
            if (_hasMesh) return;

            // DontSave: a malha é sempre gerada, nunca gravada na cena
            Mesh = new Mesh { name = MeshName, hideFlags = HideFlags.DontSave };
            _meshFilter.sharedMesh = Mesh;
            _hasMesh = true;
        }

        private void Clear()
        {
            _skeleton.Clear();
            VertexCount = 0;
            TriangleCount = 0;
            IsGenerated = false;
            if (_hasMesh) Mesh.Clear();
        }

        private void DestroyMesh()
        {
            if (!_hasMesh) return;

            if (Application.isPlaying) Destroy(Mesh);
            else DestroyImmediate(Mesh);
            Mesh = null;
            _hasMesh = false;
            IsGenerated = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // OnValidate não pode mexer em componentes (MeshCollider): regenera no próximo tick do editor
            EditorApplication.delayCall -= HandleDelayedValidate;
            EditorApplication.delayCall += HandleDelayedValidate;
        }

        private void HandleDelayedValidate()
        {
            // A árvore pode ter sido apagada antes do delayCall rodar
            if (this == null || !isActiveAndEnabled) return;

            _hasSpecies = _species != null;
            UnsubscribeFromSpecies();
            SubscribeToSpecies();
            Generate();
        }

        private void HandleSpeciesChanged()
        {
            EditorApplication.delayCall -= HandleDelayedValidate;
            EditorApplication.delayCall += HandleDelayedValidate;
        }

        private void SubscribeToSpecies()
        {
            if (!_hasSpecies) return;

            _subscribedSpecies = _species;
            _hasSubscribedSpecies = true;
            _subscribedSpecies.Changed += HandleSpeciesChanged;
        }

        private void UnsubscribeFromSpecies()
        {
            if (!_hasSubscribedSpecies) return;

            // A espécie pode ter sido apagada do projeto; aí não há de quem se desinscrever
            if (_subscribedSpecies != null) _subscribedSpecies.Changed -= HandleSpeciesChanged;
            _subscribedSpecies = null;
            _hasSubscribedSpecies = false;
        }

        // Eixos de tronco e galhos (selecione a árvore para ver)
        private void OnDrawGizmosSelected()
        {
            if (!IsGenerated) return;

            Gizmos.matrix = transform.localToWorldMatrix;
            Gizmos.color = Color.yellow;
            for (int b = 0; b < _skeleton.BranchCount; b++)
            {
                TreeBranch branch = _skeleton.GetBranch(b);
                for (int n = branch.FirstNode; n < branch.LastNode; n++)
                {
                    Gizmos.DrawLine(_skeleton.GetNode(n).Position, _skeleton.GetNode(n + 1).Position);
                }
            }
        }
#endif
    }
}
