using LumberJack.Wood;
using UnityEngine;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace LumberJack.Trees
{
    /// <summary>
    /// Tora solta na cena: no editor mostra a tora; no Play vira um <see cref="TreePiece"/> (o mesmo pedaço das
    /// árvores derrubadas, com física, golpes e anéis) e este objeto some. A tora fica no eixo Y local.
    /// </summary>
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter), typeof(MeshRenderer))]
    public class ProceduralLog : MonoBehaviour
    {
        private const string PreviewMeshName = "ProceduralLog";
        private const float MinLength = 0.3f;
        private const float MinRadius = 0.05f;

        // Toras são montadas uma por vez na thread principal: um montador para todas
        private static readonly TreeLogBuilder SharedBuilder = new();

        [Tooltip("Espécie: casca, anéis e física dos pedaços")]
        [SerializeField] private TreeSpeciesDefinition _species;
        [Tooltip("Tipo de madeira (preço); vazio usa o da espécie")]
        [SerializeField] private WoodDefinition _wood;
        [Tooltip("Comprimento (m)")]
        [Min(MinLength)]
        [SerializeField] private float _length = 3f;
        [Tooltip("Raio (m)")]
        [Min(MinRadius)]
        [SerializeField] private float _radius = 0.3f;
        [Tooltip("Muda as irregularidades da casca")]
        [SerializeField] private int _seed = 1;

        private readonly TreeSkeleton _previewSkeleton = new();
        private Mesh _previewMesh;
        private bool _hasPreview;

        private void OnEnable()
        {
            if (!Application.isPlaying) BuildPreview();
        }

        private void Start()
        {
            if (Application.isPlaying) SpawnPiece();
        }

        private void OnDisable()
        {
            DestroyPreview();
        }

        private void SpawnPiece()
        {
            if (_species == null)
            {
                Debug.LogError($"{nameof(ProceduralLog)} '{name}' sem espécie: a tora não foi criada.", this);
                return;
            }

            // Cada pedaço guarda o próprio esqueleto (as metades dos cortes usam o mesmo)
            var skeleton = new TreeSkeleton();
            Mesh mesh = SharedBuilder.Build(_species, _length, _radius, _seed, skeleton);
            if (mesh == null) return;

            WoodDefinition wood = _wood != null ? _wood : _species.Wood;
            TreePiece.Spawn(transform, name, mesh, skeleton, _species, wood);
            Destroy(gameObject);
        }

        private void BuildPreview()
        {
            DestroyPreview();
            if (_species == null) return;

            _previewMesh = SharedBuilder.Build(_species, _length, _radius, _seed, _previewSkeleton);
            if (_previewMesh == null) return;

            // DontSave: a malha é sempre gerada, nunca gravada na cena
            _previewMesh.name = PreviewMeshName;
            _previewMesh.hideFlags = HideFlags.DontSave;
            _hasPreview = true;
            GetComponent<MeshFilter>().sharedMesh = _previewMesh;
            GetComponent<MeshRenderer>().sharedMaterials = new[] { _species.BarkMaterial, _species.CutFaceMaterial };
        }

        private void DestroyPreview()
        {
            if (!_hasPreview) return;

            if (Application.isPlaying) Destroy(_previewMesh);
            else DestroyImmediate(_previewMesh);
            _previewMesh = null;
            _hasPreview = false;
        }

#if UNITY_EDITOR
        private void OnValidate()
        {
            // OnValidate não pode trocar malha e materiais: refaz a prévia no próximo tick do editor
            EditorApplication.delayCall -= HandleDelayedValidate;
            EditorApplication.delayCall += HandleDelayedValidate;
        }

        private void HandleDelayedValidate()
        {
            // A tora pode ter sido apagada (ou o Play começou) antes do delayCall rodar
            if (this == null || !isActiveAndEnabled || Application.isPlaying) return;

            BuildPreview();
        }
#endif
    }
}
