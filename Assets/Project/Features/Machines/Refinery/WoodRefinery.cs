using LumberJack.Wood;
using TMPro;
using UnityEngine;

namespace LumberJack.Machines
{
    public enum RefineryDimension
    {
        Width,
        Height
    }

    /// <summary>
    /// Transforma madeira bruta em tábua. A largura e a altura da tábua são ajustadas pelos botões
    /// da máquina; o comprimento sai do volume da madeira que entrou (menos a perda).
    /// O estado fica aqui em passos inteiros: as telas só mostram o valor.
    /// </summary>
    public class WoodRefinery : MonoBehaviour
    {
        private const string WoodTag = "Wood";
        private const string ValueFormat = "{0:1}";

        [Header("Ajuste do tamanho")]
        [Tooltip("Quanto cada clique muda o valor mostrado")]
        [SerializeField] private float _step = 0.2f;
        [SerializeField] private float _minValue = 1f;
        [SerializeField] private float _maxValue = 3f;
        [Tooltip("Valor inicial das telas; deve bater com o tamanho do preview na cena")]
        [SerializeField] private float _startValue = 3f;
        [Tooltip("Fração do comprimento perdida ao refinar (0.1 = 10%)")]
        [SerializeField] private float _lossFraction = 0.1f;

        [Header("Referências")]
        [SerializeField] private Transform _spawnPoint;
        [SerializeField] private GameObject _boardPrefab;
        [SerializeField] private TextMeshPro _widthText;
        [SerializeField] private TextMeshPro _heightText;
        [Tooltip("Tábua de exemplo na máquina; o tamanho dela vira o tamanho da tábua gerada")]
        [SerializeField] private GameObject _boardPreview;

        private Transform _previewTransform;
        private Vector3 _previewStartSize;
        private float _previewStep;
        private int _stepCount;
        private int _startSteps;
        private int _widthSteps;
        private int _heightSteps;

        private void Awake()
        {
            _stepCount = Mathf.RoundToInt((_maxValue - _minValue) / _step);
            _startSteps = Mathf.RoundToInt((_startValue - _minValue) / _step);
            _widthSteps = _startSteps;
            _heightSteps = _startSteps;

            _previewTransform = _boardPreview.transform;
            _previewStartSize = _previewTransform.localScale;
            _previewStep = _step / _stepCount;
            UpdateDisplay();
        }

        private void OnTriggerEnter(Collider other)
        {
            // Pedaços de árvore colidem com cascos filhos: a madeira fica no objeto do Rigidbody
            Rigidbody body = other.attachedRigidbody;
            if (ReferenceEquals(body, null) || !body.CompareTag(WoodTag)) return;
            if (!body.TryGetComponent(out WoodPiece wood) || wood.IsRefined || !wood.TryConsume()) return;

            Refine(wood.Definition, wood.Volume);
            Destroy(body.gameObject);
        }

        /// <summary>
        /// Muda a largura ou a altura em um passo (direction = +1 aumenta, -1 diminui).
        /// </summary>
        public void Adjust(RefineryDimension dimension, int direction)
        {
            if (dimension == RefineryDimension.Width) _widthSteps = Mathf.Clamp(_widthSteps + direction, 0, _stepCount);
            else _heightSteps = Mathf.Clamp(_heightSteps + direction, 0, _stepCount);
            UpdateDisplay();
        }

        private void UpdateDisplay()
        {
            _widthText.SetText(ValueFormat, _minValue + _widthSteps * _step);
            _heightText.SetText(ValueFormat, _minValue + _heightSteps * _step);

            Vector3 size = _previewStartSize;
            size.z += (_widthSteps - _startSteps) * _previewStep;
            size.y += (_heightSteps - _startSteps) * _previewStep;
            _previewTransform.localScale = size;
        }

        private void Refine(WoodDefinition definition, float rawVolume)
        {
            GameObject board = Instantiate(_boardPrefab, _spawnPoint.position, Quaternion.identity);
            board.transform.localScale = GetBoardSize(rawVolume);
            board.GetComponent<WoodPiece>().Initialize(definition, true);
        }

        // A seção da tábua é a do preview; o comprimento conserva o volume (menos a perda)
        private Vector3 GetBoardSize(float volume)
        {
            Vector3 preview = _previewTransform.localScale;
            float length = volume / (preview.y * preview.z) * (1f - _lossFraction);
            return new Vector3(preview.z, preview.y, length);
        }
    }
}
