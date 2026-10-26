using LumberJack.WorldTime;
using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Toco apodrecendo (uma vez por hora do jogo): escurece e ganha tom de musgo, encolhe um pouco e, na reta final,
    /// afunda no chão até sumir e liberar o lugar. A cor muda por renderer (o material compartilhado não muda).
    /// </summary>
    public class StumpDecay : MonoBehaviour
    {
        // Cor da madeira podre: multiplica a cor original (casca e anéis do corte)
        private static readonly Color RotTint = new(0.42f, 0.46f, 0.3f);
        private static readonly int BaseColorId = Shader.PropertyToID("_BaseColor");
        // No fim o toco fica com essa fração do tamanho
        private const float EndScale = 0.85f;
        // Só afunda no fim: até aqui ele só apodrece no lugar
        private const float SinkStart = 0.6f;

        private WorldClock _clock;
        private Transform _transform;
        private Renderer _renderer;
        private MaterialPropertyBlock _block;
        private Color[] _baseColors;
        private bool _hasRenderer;
        private double _felledHours;
        private double _decayHours;
        private Vector3 _startPosition;
        private Vector3 _startScale;
        private float _sinkDepth;
        private bool _isStarted;

        /// <summary>Quanto já apodreceu (0 = recém-cortado, 1 = some).</summary>
        public float Progress { get; private set; }

        private void OnEnable()
        {
            if (_isStarted) _clock.HourChanged += HandleHourChanged;
        }

        private void OnDisable()
        {
            if (_isStarted) _clock.HourChanged -= HandleHourChanged;
        }

        /// <summary>Começa a apodrecer a partir de <paramref name="felledHours"/> (horas do relógio).</summary>
        public void Begin(WorldClock clock, double felledHours, float decayDays)
        {
            _clock = clock;
            _transform = transform;
            _felledHours = felledHours;
            _decayHours = decayDays * GameCalendar.HoursPerDay;
            _startPosition = _transform.position;
            _startScale = _transform.localScale;
            _hasRenderer = TryGetComponent(out _renderer);
            if (_hasRenderer)
            {
                // Afunda a altura do toco inteiro
                _sinkDepth = _renderer.bounds.size.y;
                _block = new MaterialPropertyBlock();
                Material[] materials = _renderer.sharedMaterials;
                _baseColors = new Color[materials.Length];
                for (int i = 0; i < materials.Length; i++)
                {
                    _baseColors[i] = materials[i].HasProperty(BaseColorId) ? materials[i].GetColor(BaseColorId) : Color.white;
                }
            }
            _isStarted = true;
            if (isActiveAndEnabled) _clock.HourChanged += HandleHourChanged;
        }

        private void HandleHourChanged()
        {
            float progress = Mathf.Clamp01((float)((_clock.TotalHours - _felledHours) / _decayHours));
            Progress = progress;
            if (progress >= 1f)
            {
                Destroy(gameObject);
                return;
            }

            float sink = Mathf.SmoothStep(0f, 1f, Mathf.InverseLerp(SinkStart, 1f, progress));
            _transform.position = _startPosition + Vector3.down * (_sinkDepth * sink);
            _transform.localScale = _startScale * Mathf.Lerp(1f, EndScale, progress);
            if (_hasRenderer) Tint(Mathf.SmoothStep(0f, 1f, progress));
        }

        private void Tint(float rot)
        {
            for (int i = 0; i < _baseColors.Length; i++)
            {
                _block.SetColor(BaseColorId, Color.Lerp(_baseColors[i], _baseColors[i] * RotTint, rot));
                _renderer.SetPropertyBlock(_block, i);
            }
        }
    }
}
