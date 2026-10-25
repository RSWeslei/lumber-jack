using System;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Braços em primeira pessoa: os clipes animam os ossos do braço e a ferramenta vai presa na mão (ToolSocket).
    /// Toca o golpe, avisa o instante da batida (curva "Impact" do clipe) e balança com o movimento.
    /// </summary>
    public class FirstPersonArms : MonoBehaviour
    {
        private const float ImpactThreshold = 0.5f;
        private const int BaseLayer = 0;

        private static readonly int SwingTriggerHash = Animator.StringToHash("Swing");
        private static readonly int SwingSpeedHash = Animator.StringToHash("SwingSpeed");
        private static readonly int ImpactHash = Animator.StringToHash("Impact");
        private static readonly int SwingStateHash = Animator.StringToHash("Swing");

        [Tooltip("Animator do modelo dos braços")]
        [SerializeField] private Animator _animator;

        [Header("Batida")]
        [Tooltip("Quanto tempo (s) o golpe trava ao acertar a madeira")]
        [Min(0f)]
        [SerializeField] private float _hitStopDuration = 0.06f;

        [Header("Balanço")]
        [Tooltip("Quanto (m) os braços ficam para trás do movimento")]
        [Min(0f)]
        [SerializeField] private float _swayAmount = 0.015f;
        [Tooltip("Rapidez com que o balanço segue o movimento")]
        [Min(0f)]
        [SerializeField] private float _swaySmoothing = 6f;
        [Tooltip("Altura (m) do sobe-e-desce ao andar")]
        [Min(0f)]
        [SerializeField] private float _bobHeight = 0.01f;
        [Tooltip("Passos por segundo do sobe-e-desce")]
        [Min(0f)]
        [SerializeField] private float _bobFrequency = 1.8f;

        private Transform _transform;
        private Vector3 _restPosition;
        private Vector3 _sway;
        private float _bobPhase;
        private bool _hasEnteredSwing;
        private bool _hasImpacted;

        public bool IsSwinging { get; private set; }

        /// <summary>Instante em que a ferramenta chega no alvo (o dano entra aqui).</summary>
        public event Action Impacted;

        private void Awake()
        {
            _transform = transform;
            _restPosition = _transform.localPosition;
        }

        private void Update()
        {
            UpdateSway();
            if (IsSwinging) UpdateSwing();
        }

        /// <summary>Começa um golpe; <paramref name="speed"/> multiplica a velocidade do clipe.</summary>
        public void Swing(float speed)
        {
            if (IsSwinging) return;

            IsSwinging = true;
            _hasEnteredSwing = false;
            _hasImpacted = false;
            _animator.SetFloat(SwingSpeedHash, speed);
            _animator.SetTrigger(SwingTriggerHash);
        }

        /// <summary>Trava o golpe um instante: a madeira "segura" o machado.</summary>
        public async void HitStop()
        {
            _animator.speed = 0f;
            try
            {
                await Awaitable.WaitForSecondsAsync(_hitStopDuration, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // Os braços foram destruídos durante a trava
                return;
            }
            _animator.speed = 1f;
        }

        private void UpdateSwing()
        {
            // A curva vem do quadro anterior do Animator: um quadro de atraso não aparece
            if (!_hasImpacted && _animator.GetFloat(ImpactHash) > ImpactThreshold)
            {
                _hasImpacted = true;
                Impacted?.Invoke();
            }

            bool isInSwing = _animator.GetCurrentAnimatorStateInfo(BaseLayer).shortNameHash == SwingStateHash;
            if (isInSwing) _hasEnteredSwing = true;
            else if (_hasEnteredSwing && !_animator.IsInTransition(BaseLayer)) IsSwinging = false;
        }

        // Os braços ficam um pouco para trás do movimento e sobem e descem com os passos
        private void UpdateSway()
        {
            Vector2 move = InputManager.Instance.MoveInput;
            float speed = Mathf.Min(move.magnitude, 1f);
            _bobPhase += speed * _bobFrequency * Mathf.PI * 2f * Time.deltaTime;

            Vector3 target = new(-move.x * _swayAmount, Mathf.Sin(_bobPhase * 2f) * _bobHeight * speed, -move.y * _swayAmount);
            _sway = Vector3.Lerp(_sway, target, Time.deltaTime * _swaySmoothing);
            _transform.localPosition = _restPosition + _sway;
        }
    }
}
