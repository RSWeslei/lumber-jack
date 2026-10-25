using System;
using System.Collections.Generic;
using LumberJack.Audio;
using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Carrega rigidbodies enquanto a tecla de pegar está apertada: o ponto agarrado vai para a frente da mira pela
    /// física e o objeto gira junto com o jogador. Só levanta até a força do jogador (PlayerStats): mais pesado que
    /// isso, o objeto fica só "agarrado" sem sair do lugar. Quanto mais perto do limite, mais devagar o objeto acompanha.
    /// Madeira só com a mão livre (sem item equipado).
    /// </summary>
    [RequireComponent(typeof(PlayerStats))]
    public class PickupObjects : MonoBehaviour
    {
        private const float MinHoldDistance = 1f;
        private const string WoodTag = "Wood";

        [Tooltip("Camada(s) que podem ser agarradas")]
        [SerializeField] private LayerMask _layerMask;
        [Tooltip("Câmera do jogador (com o PlayerAim)")]
        [SerializeField] private Transform _cameraTransform;
        [Tooltip("Mão com o item equipado: segurando item, não pega madeira")]
        [SerializeField] private Hand _hand;

        [Header("Carregar")]
        [Tooltip("Solta o objeto se o ponto agarrado ficar mais longe que isso (m) de onde deveria estar")]
        [SerializeField] private float _maxDistance = 1.5f;
        [Tooltip("Quanto mais alto, mais rápido o objeto alcança a mira")]
        [SerializeField] private float _followStrength = 12f;
        [Tooltip("Velocidade máxima (m/s) de um objeto leve")]
        [SerializeField] private float _maxCarrySpeed = 8f;
        [Tooltip("Fração da velocidade máxima com o peso no limite da força")]
        [Range(0.05f, 1f)]
        [SerializeField] private float _fullLoadSpeedFraction = 0.35f;

        [Header("Sons")]
        [SerializeField] private AudioSource _audio;
        [Tooltip("Mãos pegando o objeto")]
        [SerializeField] private SoundDefinition _grabSound;

        [Header("Giro")]
        [Tooltip("Quanto mais alto, mais rápido o objeto acompanha o giro do jogador")]
        [SerializeField] private float _turnStrength = 10f;
        [Tooltip("Giro máximo (graus/s) do objeto carregado")]
        [SerializeField] private float _maxTurnSpeed = 270f;

        private PlayerStats _stats;
        private PlayerAim _aim;
        private Collider[] _ownColliders;
        private readonly List<Collider> _carriedColliders = new();

        private Rigidbody _heldBody;
        private Vector3 _localGrabPoint;
        private Quaternion _rotationFromView;
        private float _holdDistance;
        private float _heldMass;
        private bool _previousUseGravity;

        /// <summary>Segurando a tecla com algo agarrado (carregando ou pesado demais para levantar).</summary>
        public bool IsHolding { get; private set; }
        /// <summary>O objeto agarrado foi levantado (cabe na força do jogador).</summary>
        public bool IsCarrying { get; private set; }
        /// <summary>Peso carregado em relação à força (0 sem carregar, 1 no limite).</summary>
        public float Load { get; private set; }

        private Vector3 GrabPoint => _heldBody.transform.TransformPoint(_localGrabPoint);

        /// <summary>Começou ou parou de segurar algo.</summary>
        public event Action HoldChanged;

        private void Awake()
        {
            _stats = GetComponent<PlayerStats>();
            _aim = _cameraTransform.GetComponent<PlayerAim>();
            _ownColliders = GetComponentsInChildren<Collider>(true);
        }

        private void OnEnable()
        {
            InputManager.Instance.GrabPressed += HandleGrabPressed;
            InputManager.Instance.GrabReleased += HandleGrabReleased;
        }

        private void FixedUpdate()
        {
            if (!IsHolding) return;

            // O objeto pode ter sido destruído enquanto era segurado (vendido, refinado, cortado)
            if (!_heldBody)
            {
                Clear();
                return;
            }

            if (!IsCarrying)
            {
                // Pesado demais: só solta se o jogador se afastar
                if (Vector3.Distance(_cameraTransform.position, GrabPoint) > _aim.Reach + _maxDistance) Release();
                return;
            }

            if (!FollowAim()) Release();
            else TurnWithView();
        }

        private void OnDisable()
        {
            InputManager.Instance.GrabPressed -= HandleGrabPressed;
            InputManager.Instance.GrabReleased -= HandleGrabReleased;
            Release();
        }

        /// <summary>O jogador está segurando este corpo agora.</summary>
        public bool IsHeld(Rigidbody body)
        {
            return IsHolding && ReferenceEquals(body, _heldBody);
        }

        /// <summary>Ponto agarrado no mundo; false sem nada agarrado.</summary>
        public bool TryGetGrabPoint(out Vector3 point)
        {
            // O objeto pode ser destruído entre um FixedUpdate e outro (cortado, vendido)
            bool hasPoint = IsHolding && _heldBody;
            point = hasPoint ? GrabPoint : Vector3.zero;
            return hasPoint;
        }

        // Leva o ponto agarrado para a frente da mira; false se ele ficou preso longe demais
        private bool FollowAim()
        {
            Vector3 target = _cameraTransform.position + _cameraTransform.forward * _holdDistance;
            Vector3 toTarget = target - GrabPoint;
            float distance = toTarget.magnitude;
            if (distance > _maxDistance) return false;

            float maxSpeed = _maxCarrySpeed * Mathf.Lerp(1f, _fullLoadSpeedFraction, Load);
            float speed = Mathf.Min(distance * _followStrength, maxSpeed);
            _heldBody.linearVelocity = distance > 0f ? toTarget / distance * speed : Vector3.zero;
            return true;
        }

        // Mantém o giro do objeto em relação ao jogador (só o giro horizontal da câmera conta)
        private void TurnWithView()
        {
            Quaternion target = ViewYaw() * _rotationFromView;
            Quaternion delta = target * Quaternion.Inverse(_heldBody.rotation);
            delta.ToAngleAxis(out float angle, out Vector3 axis);
            if (angle > 180f) angle -= 360f;

            float turnSpeed = Mathf.Clamp(angle * _turnStrength, -_maxTurnSpeed, _maxTurnSpeed);
            _heldBody.angularVelocity = float.IsFinite(axis.x) ? axis * (turnSpeed * Mathf.Deg2Rad) : Vector3.zero;
        }

        private Quaternion ViewYaw()
        {
            return Quaternion.Euler(0f, _cameraTransform.eulerAngles.y, 0f);
        }

        private void HandleGrabPressed()
        {
            if (IsHolding || !_aim.TryFindTarget(_layerMask, out RaycastHit hit)) return;

            Rigidbody body = hit.rigidbody;
            if (body == null || body.isKinematic) return;
            if (_hand.HasItem && body.CompareTag(WoodTag)) return;

            _heldBody = body;
            _localGrabPoint = body.transform.InverseTransformPoint(hit.point);
            _heldMass = body.mass;
            IsHolding = true;
            IsCarrying = _heldMass <= _stats.CarryCapacityKg;
            if (IsCarrying) Lift(hit.distance);
            HoldChanged?.Invoke();
        }

        private void Lift(float grabDistance)
        {
            float capacity = _stats.CarryCapacityKg;
            Load = capacity > 0f ? Mathf.Clamp01(_heldMass / capacity) : 1f;
            _holdDistance = Mathf.Clamp(grabDistance, MinHoldDistance, _aim.Reach);
            _rotationFromView = Quaternion.Inverse(ViewYaw()) * _heldBody.rotation;

            _previousUseGravity = _heldBody.useGravity;
            _heldBody.useGravity = false;
            // O objeto não empurra nem levanta o próprio jogador
            _heldBody.GetComponentsInChildren(_carriedColliders);
            SetIgnoreOwnColliders(true);
            _grabSound.Play(_audio);
        }

        private void HandleGrabReleased()
        {
            Release();
        }

        private void Release()
        {
            if (!IsHolding) return;

            if (IsCarrying && _heldBody)
            {
                _heldBody.useGravity = _previousUseGravity;
                SetIgnoreOwnColliders(false);
            }
            Clear();
        }

        private void Clear()
        {
            _heldBody = null;
            _carriedColliders.Clear();
            IsHolding = false;
            IsCarrying = false;
            Load = 0f;
            HoldChanged?.Invoke();
        }

        private void SetIgnoreOwnColliders(bool ignore)
        {
            foreach (Collider carried in _carriedColliders)
            {
                // Cascos podem ter sido trocados enquanto carregava
                if (!carried) continue;
                foreach (Collider own in _ownColliders) Physics.IgnoreCollision(carried, own, ignore);
            }
        }
    }
}
