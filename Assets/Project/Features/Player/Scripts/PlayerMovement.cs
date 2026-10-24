using System;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    // Movimento em primeira pessoa com CharacterController: aceleração, coyote time,
    // jump buffer e pulo variável (soltar o botão corta o pulo).
    [RequireComponent(typeof(CharacterController))]
    public class PlayerMovement : MonoBehaviour
    {
        [Header("Velocidade (m/s)")]
        [SerializeField] private float _walkSpeed = 4.5f;
        [SerializeField] private float _sprintSpeed = 7f;
        [Tooltip("Andar de costas é mais lento")]
        [SerializeField] private float _backwardMultiplier = 0.75f;

        [Header("Aceleração (m/s2)")]
        [SerializeField] private float _groundAcceleration = 45f;
        [SerializeField] private float _groundDeceleration = 55f;
        [SerializeField] private float _airAcceleration = 12f;
        [Tooltip("Perda de velocidade no ar sem input (0 = mantém o embalo)")]
        [SerializeField] private float _airDrag = 1.5f;

        [Header("Pulo e gravidade")]
        [SerializeField] private float _jumpHeight = 1.1f;
        [SerializeField] private float _gravity = -22f;
        [Tooltip("Gravidade extra na descida, deixa o pulo menos 'flutuante'")]
        [SerializeField] private float _fallGravityMultiplier = 1.6f;
        [Tooltip("Soltar o pulo cedo multiplica a velocidade de subida por isso")]
        [SerializeField] private float _jumpCutMultiplier = 0.5f;
        [Tooltip("Tempo para ainda pular depois de sair da beirada")]
        [SerializeField] private float _coyoteTime = 0.12f;
        [Tooltip("Apertar pulo um pouco antes de tocar o chão ainda conta")]
        [SerializeField] private float _jumpBufferTime = 0.15f;
        [SerializeField] private float _terminalVelocity = 50f;

        [Header("Chão")]
        [Tooltip("Velocidade para baixo que mantém o player grudado no chão e em rampas")]
        [SerializeField] private float _groundStickVelocity = -4f;
        [SerializeField] private float _groundProbeDistance = 0.2f;

        private CharacterController _controller;
        private Vector3 _horizontalVelocity;
        private float _verticalVelocity;
        private Vector3 _groundNormal = Vector3.up;
        private float _coyoteTimer;
        private float _jumpBufferTimer;
        private bool _isJumping;
        private readonly RaycastHit[] _probeHits = new RaycastHit[8];
        // Colisores do próprio player, ignorados pela sonda do chão
        private Collider[] _ownColliders;

        private Vector2 _moveInput;
        private bool _jumpPressed;
        private bool _jumpHeld;
        private bool _sprintHeld;

        public bool IsGrounded { get; private set; }
        public bool IsSprinting { get; private set; }
        public Vector2 MoveInput => _moveInput;
        public float WalkSpeed => _walkSpeed;
        public float HorizontalSpeed => new Vector3(_controller.velocity.x, 0f, _controller.velocity.z).magnitude;

        /// <summary>Disparado ao tocar o chão, com a velocidade de queda (m/s, positiva).</summary>
        public event Action<float> OnLanded;
        public event Action OnJumped;

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _ownColliders = GetComponentsInChildren<Collider>(true);
        }

        private void Update()
        {
            ReadInput();
            float deltaTime = Time.deltaTime;

            UpdateHorizontalVelocity(deltaTime);
            UpdateJumpAndGravity(deltaTime);
            Move(deltaTime);
        }

        private void ReadInput()
        {
            PlayerInputs.PlayerMovementActions actions = InputManager.Instance.PlayerInputs.PlayerMovement;
            _moveInput = Vector2.ClampMagnitude(actions.Move.ReadValue<Vector2>(), 1f);
            _jumpPressed = actions.Jump.WasPressedThisFrame();
            _jumpHeld = actions.Jump.IsPressed();
            _sprintHeld = actions.Sprint.IsPressed();
        }

        private void UpdateHorizontalVelocity(float deltaTime)
        {
            // Correr só para frente (não de lado nem de costas)
            IsSprinting = _sprintHeld && _moveInput.y > 0.1f;

            float targetSpeed = IsSprinting ? _sprintSpeed : _walkSpeed;
            if (_moveInput.y < 0f) targetSpeed *= _backwardMultiplier;

            Vector3 wishDirection = transform.right * _moveInput.x + transform.forward * _moveInput.y;
            Vector3 targetVelocity = wishDirection * targetSpeed;
            bool hasInput = _moveInput.sqrMagnitude > 0.0001f;

            float rate;
            if (IsGrounded) rate = hasInput ? _groundAcceleration : _groundDeceleration;
            else rate = hasInput ? _airAcceleration : _airDrag;

            // MoveTowards no vetor (e não só na velocidade) deixa a troca de direção firme
            _horizontalVelocity = Vector3.MoveTowards(_horizontalVelocity, targetVelocity, rate * deltaTime);
        }

        private void UpdateJumpAndGravity(float deltaTime)
        {
            _coyoteTimer = IsGrounded ? _coyoteTime : _coyoteTimer - deltaTime;
            _jumpBufferTimer = _jumpPressed ? _jumpBufferTime : _jumpBufferTimer - deltaTime;

            if (_jumpBufferTimer > 0f && _coyoteTimer > 0f)
            {
                _verticalVelocity = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
                _jumpBufferTimer = 0f;
                _coyoteTimer = 0f;
                _isJumping = true;
                IsGrounded = false;
                OnJumped?.Invoke();
            }

            // Pulo variável: soltar o botão durante a subida corta o pulo uma vez
            if (_isJumping && !_jumpHeld && _verticalVelocity > 0f)
            {
                _verticalVelocity *= _jumpCutMultiplier;
                _isJumping = false;
            }

            if (IsGrounded && _verticalVelocity <= 0f)
            {
                _verticalVelocity = _groundStickVelocity;
                _isJumping = false;
                return;
            }

            float gravity = _verticalVelocity < 0f ? _gravity * _fallGravityMultiplier : _gravity;
            _verticalVelocity = Mathf.Max(_verticalVelocity + gravity * deltaTime, -_terminalVelocity);
        }

        private void Move(float deltaTime)
        {
            Vector3 horizontal = _horizontalVelocity;

            // No chão, segue a inclinação da rampa: descer rampa não fica "pulando"
            if (IsGrounded && horizontal.sqrMagnitude > 0f)
            {
                horizontal = Vector3.ProjectOnPlane(horizontal, _groundNormal).normalized * horizontal.magnitude;
            }

            float fallSpeed = -_verticalVelocity;
            CollisionFlags flags = _controller.Move((horizontal + Vector3.up * _verticalVelocity) * deltaTime);

            // Bateu a cabeça no teto: para de subir
            if ((flags & CollisionFlags.Above) != 0 && _verticalVelocity > 0f)
            {
                _verticalVelocity = 0f;
            }

            bool wasGrounded = IsGrounded;
            bool groundBelow = ProbeGround();
            IsGrounded = _verticalVelocity <= 0f && (_controller.isGrounded || groundBelow);
            if (!groundBelow) _groundNormal = Vector3.up;

            if (!wasGrounded && IsGrounded)
            {
                OnLanded?.Invoke(Mathf.Max(0f, fallSpeed));
            }
            else if (wasGrounded && !IsGrounded && !_isJumping)
            {
                // Saiu andando de uma beirada: começa a cair do zero, sem o "grude" no chão
                _verticalVelocity = 0f;
            }
        }

        // Procura o chão logo abaixo dos pés (ignorando os próprios colisores) e guarda a normal
        private bool ProbeGround()
        {
            float radius = _controller.radius * 0.95f;
            Vector3 origin = transform.position + _controller.center
                + Vector3.down * (_controller.height * 0.5f - _controller.radius);
            float distance = _groundProbeDistance + _controller.skinWidth;

            int count = Physics.SphereCastNonAlloc(origin, radius, Vector3.down, _probeHits, distance,
                Physics.AllLayers, QueryTriggerInteraction.Ignore);

            float closest = float.MaxValue;
            bool found = false;
            for (int i = 0; i < count; i++)
            {
                RaycastHit hit = _probeHits[i];
                if (hit.distance <= 0f || IsOwnCollider(hit.collider)) continue;
                if (Vector3.Angle(hit.normal, Vector3.up) > _controller.slopeLimit) continue;
                if (hit.distance < closest)
                {
                    closest = hit.distance;
                    _groundNormal = hit.normal;
                    found = true;
                }
            }
            return found;
        }

        private bool IsOwnCollider(Collider candidate)
        {
            foreach (Collider own in _ownColliders)
            {
                if (ReferenceEquals(own, candidate)) return true;
            }
            return false;
        }
    }
}
