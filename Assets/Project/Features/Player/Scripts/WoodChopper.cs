using System;
using System.Threading;
using LumberJack.Core;
using LumberJack.Tools;
using LumberJack.Wood;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Player
{
    /// <summary>
    /// Machado equipado na hotbar: corta toras (com a linha do corte) e golpeia árvores em pé.
    /// </summary>
    public class WoodChopper : MonoBehaviour
    {
        private const string WoodLayerName = "Wood";
        private const float WindUpDuration = 0.15f;
        private const float StrikeDuration = 0.08f;
        private const float RecoverDuration = 0.25f;

        [SerializeField] private Hand _hand;
        [SerializeField] private Material _previewMaterial;

        [Header("Inclinação do corte")]
        [SerializeField] private float _maxTilt = 60f;
        [Tooltip("Graus por segundo segurando a tecla de inclinar")]
        [SerializeField] private float _tiltSpeed = 60f;

        [Header("Golpe")]
        [SerializeField] private float _windUpAngle = -60f;
        [SerializeField] private float _strikeAngle = 40f;

        private Transform _camera;
        private PlayerAim _aim;
        private Transform _handTransform;
        private Transform _handParent;
        private bool _hasHandParent;
        private int _woodMask;
        private CutPreview _preview;
        private InputAction _tiltAction;
        private InputAction _straightenAction;
        private float _tilt;
        private bool _isSwinging;

        // Cache do último collider mirado: evita GetComponent a cada frame no mesmo tronco
        private Collider _lastCollider;
        private IAxeTarget _lastColliderTarget;
        private bool _lastColliderIsTarget;

        private IAxeTarget _target;
        private bool _hasTarget;
        private Plane _targetPlane;
        private bool _isTargetValid;

        private bool HasAxe => _hand.EquippedItem is ToolDefinition tool && tool.CanCutWood;

        private void Awake()
        {
            if (_hand == null)
            {
                Debug.LogError($"{nameof(WoodChopper)} sem referência para a mão.", this);
                enabled = false;
                return;
            }

            _handTransform = _hand.transform;
            _handParent = _handTransform.parent;
            _hasHandParent = _handParent != null;
            _aim = GetComponentInParent<PlayerAim>();
            _camera = _aim.transform;
            _woodMask = LayerMask.GetMask(WoodLayerName);
            _preview = CutPreview.Create(transform, _previewMaterial);

            PlayerInputs.PlayerActionsActions actions = InputManager.Instance.PlayerInputs.PlayerActions;
            _tiltAction = actions.CutTilt;
            _straightenAction = actions.CutStraighten;
        }

        private void OnEnable()
        {
            InputManager.Instance.AttackPressed += HandleAttackPressed;
        }

        private void OnDisable()
        {
            InputManager.Instance.AttackPressed -= HandleAttackPressed;
        }

        private void Update()
        {
            _hasTarget = false;
            if (!HasAxe || _isSwinging)
            {
                _preview.Hide();
                return;
            }

            UpdateTilt();
            UpdateTarget();

            if (_hasTarget) _preview.Show(_target, _targetPlane, _isTargetValid, _target.CutProgress);
            else _preview.Hide();
        }

        private void UpdateTilt()
        {
            if (_straightenAction.WasPressedThisFrame()) _tilt = 0f;
            _tilt += _tiltAction.ReadValue<float>() * _tiltSpeed * Time.deltaTime;
            _tilt = Mathf.Clamp(_tilt, -_maxTilt, _maxTilt);
        }

        private void UpdateTarget()
        {
            if (!_aim.TryFindTarget(_woodMask, out RaycastHit hit)) return;

            // ReferenceEquals compara só a referência C#, sem a checagem nativa do == do Unity
            Collider hitCollider = hit.collider;
            if (!ReferenceEquals(hitCollider, _lastCollider))
            {
                _lastCollider = hitCollider;
                _lastColliderIsTarget = TryGetAxeTarget(hitCollider, out _lastColliderTarget);
            }
            if (!_lastColliderIsTarget) return;

            _target = _lastColliderTarget;
            _hasTarget = true;
            _targetPlane = _target.GetCutPlane(hit.point, _camera.forward, _camera.up, _tilt);
            _isTargetValid = _target.CanCut(_targetPlane);
        }

        // O alvo pode estar no próprio collider (tora, árvore em pé) ou no corpo dele: pedaços de árvore
        // colidem com cápsulas filhas e o alvo fica no objeto do Rigidbody
        private static bool TryGetAxeTarget(Collider hitCollider, out IAxeTarget target)
        {
            if (hitCollider.TryGetComponent(out target)) return true;

            Rigidbody body = hitCollider.attachedRigidbody;
            // attachedRigidbody devolve null de verdade quando não há corpo: ReferenceEquals basta
            return !ReferenceEquals(body, null) && body.TryGetComponent(out target);
        }

        private async void HandleAttackPressed()
        {
            if (!HasAxe || _isSwinging) return;

            // Golpe no ar também anima, só não corta nada
            IAxeTarget target = _hasTarget && _isTargetValid ? _target : null;
            float damage = ((ToolDefinition)_hand.EquippedItem).ChopDamage;
            try
            {
                await SwingAsync(target, _targetPlane, damage, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // O jogador foi destruído no meio do golpe (saiu do Play, trocou de cena)
            }
        }

        private async Awaitable SwingAsync(IAxeTarget target, Plane localPlane, float damage, CancellationToken token)
        {
            _isSwinging = true;
            Quaternion rest = _handTransform.localRotation;

            // Gira a mão em volta do eixo "direita" da câmera: negativo levanta, positivo desce
            Vector3 axis = _hasHandParent ? _handParent.InverseTransformDirection(_camera.right) : _camera.right;

            // Se for cancelado, o jogador está sendo destruído: não há estado para restaurar
            await RotateHandAsync(rest, axis, _windUpAngle, WindUpDuration, token);
            await RotateHandAsync(rest, axis, _strikeAngle, StrikeDuration, token);

            // O alvo pode ter sido cortado/destruído enquanto o machado descia
            if (target is UnityEngine.Object targetObject && targetObject != null && target.CanCut(localPlane))
            {
                target.Strike(localPlane, _camera.forward, damage);
            }

            await RotateHandAsync(rest, axis, 0f, RecoverDuration, token);
            _handTransform.localRotation = rest;
            _isSwinging = false;
        }

        private async Awaitable RotateHandAsync(Quaternion rest, Vector3 axis, float angle, float duration, CancellationToken token)
        {
            Quaternion from = _handTransform.localRotation;
            Quaternion to = Quaternion.AngleAxis(angle, axis) * rest;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                _handTransform.localRotation = Quaternion.Slerp(from, to, time / duration);
                await Awaitable.NextFrameAsync(token);
            }
            _handTransform.localRotation = to;
        }
    }
}
