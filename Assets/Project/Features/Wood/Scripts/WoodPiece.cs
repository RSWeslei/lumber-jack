using UnityEngine;

namespace LumberJack.Wood
{
    /// <summary>
    /// Qualquer pedaco de madeira do jogo (tronco, pedaco cortado, tabua): o tipo de madeira
    /// e o valor de venda, calculado pelo volume real da malha.
    /// </summary>
    // ExecuteAlways: mostra o valor certo tambem fora do Play (prefabs e cena)
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class WoodPiece : MonoBehaviour
    {
        [SerializeField] private WoodDefinition _definition;
        [Tooltip("Ja passou pela refinaria (tabua)")]
        [SerializeField] private bool _isRefined;

        private MeshFilter _meshFilter;

        public WoodDefinition Definition => _definition;
        public bool IsRefined => _isRefined;
        /// <summary>Valor de venda em centavos.</summary>
        public int ValueCents { get; private set; }

        /// <summary>Volume real da malha em m3 (vale para cubos escalados e pedacos cortados).</summary>
        public float Volume
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return MeshSlicer.GetVolume(_meshFilter.sharedMesh) * Mathf.Abs(scale.x * scale.y * scale.z);
            }
        }

        private void Awake()
        {
            _meshFilter = GetComponent<MeshFilter>();
        }

        private void Start()
        {
            RecalculateValue();
        }

        public void Initialize(WoodDefinition definition, bool isRefined)
        {
            _definition = definition;
            _isRefined = isRefined;
            RecalculateValue();
        }

        /// <summary>
        /// Usado quando um pedaco e cortado: o pedaco novo herda o tipo de madeira.
        /// </summary>
        public void CopyFrom(WoodPiece other)
        {
            Initialize(other._definition, other._isRefined);
        }

        private void RecalculateValue()
        {
            if (_definition == null || _meshFilter.sharedMesh == null)
            {
                ValueCents = 0;
                return;
            }

            int pricePerCubicMeter = _isRefined
                ? _definition.RefinedPriceCentsPerCubicMeter
                : _definition.RawPriceCentsPerCubicMeter;
            ValueCents = Mathf.FloorToInt(Volume * pricePerCubicMeter);
        }
    }
}
