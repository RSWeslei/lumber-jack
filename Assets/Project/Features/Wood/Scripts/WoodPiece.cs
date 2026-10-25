using System.Text;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Wood
{
    /// <summary>
    /// Qualquer pedaço de madeira do jogo (tronco, pedaço cortado, tábua): o tipo de madeira
    /// e o valor de venda, calculado pelo volume real da malha.
    /// </summary>
    // ExecuteAlways: mostra o valor certo também fora do Play (prefabs e cena)
    [ExecuteAlways]
    [RequireComponent(typeof(MeshFilter))]
    public class WoodPiece : MonoBehaviour, IDebugInfo
    {
        [SerializeField] private WoodDefinition _definition;
        [Tooltip("Já passou pela refinaria (tábua)")]
        [SerializeField] private bool _isRefined;

        private MeshFilter _meshFilter;

        public WoodDefinition Definition => _definition;
        public bool IsRefined => _isRefined;
        /// <summary>Valor de venda em centavos.</summary>
        public int ValueCents { get; private set; }
        /// <summary>Já foi vendido ou refinado (o objeto some no fim do quadro).</summary>
        public bool IsConsumed { get; private set; }

        /// <summary>Volume real da malha em m3 (vale para cubos escalados e pedaços cortados).</summary>
        public float Volume
        {
            get
            {
                Vector3 scale = transform.lossyScale;
                return MeshVolume.Of(_meshFilter.sharedMesh) * Mathf.Abs(scale.x * scale.y * scale.z);
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
        /// Marca o pedaço como usado (vendido, refinado). False se já foi: cada collider do pedaço dispara o
        /// trigger, e o Destroy só acontece no fim do quadro.
        /// </summary>
        public bool TryConsume()
        {
            if (IsConsumed) return false;

            IsConsumed = true;
            return true;
        }

        public void AppendDebugInfo(StringBuilder text)
        {
            string wood = _definition != null ? _definition.DisplayName : "sem tipo";
            text.Append("Madeira: ").Append(wood).AppendLine(_isRefined ? " (tábua)" : " (bruta)");
            text.Append("Volume: ").Append(Volume.ToString("0.000")).AppendLine(" m³");
            text.Append("Valor: ").AppendLine(Money.Format(ValueCents));
        }

        /// <summary>Recalcula o valor depois que a malha mudou (galho quebrado).</summary>
        public void RecalculateValue()
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
