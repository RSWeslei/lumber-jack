using System.Collections;
using LumberJack.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Player
{
    /// <summary>
    /// Corta madeira com o machado equipado na hotbar: mira na madeira, mostra a linha do corte
    /// e corta no ataque.
    /// </summary>
    public class WoodChopper : MonoBehaviour
    {
        private const float WindUpDuration = 0.15f;
        private const float StrikeDuration = 0.08f;
        private const float RecoverDuration = 0.25f;

        [SerializeField] private Hand _hand;
        [SerializeField] private Material _previewMaterial;
        [SerializeField] private float _reach = 2.5f;

        [Header("Inclinacao do corte")]
        [SerializeField] private float _maxTilt = 60f;
        [Tooltip("Graus por segundo segurando a tecla de inclinar")]
        [SerializeField] private float _tiltSpeed = 60f;

        [Header("Golpe")]
        [SerializeField] private float _windUpAngle = -60f;
        [SerializeField] private float _strikeAngle = 40f;

        private Transform _camera;
        private CutPreview _preview;
        private InputAction _tiltAction;
        private InputAction _straightenAction;
        private float _tilt;
        private bool _isSwinging;

        // Cache do ultimo collider mirado: evita GetComponent a cada frame no mesmo tronco
        private Collider _lastCollider;
        private CuttableWood _lastColliderWood;
        private bool _lastColliderIsWood;

        private CuttableWood _target;
        private bool _hasTarget;
        private Plane _targetPlane;
        private bool _isTargetValid;

        private bool HasAxe => _hand.EquippedItem is ToolSO tool && tool.canCutWood;

        private void Awake()
        {
            if (_hand == null)
            {
                Debug.LogError($"{nameof(WoodChopper)} sem referencia para a mao.", this);
                enabled = false;
                return;
            }

            Camera parentCamera = GetComponentInParent<Camera>();
            _camera = parentCamera != null ? parentCamera.transform : Camera.main.transform;
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

            if (_hasTarget) _preview.Show(_target, _targetPlane, _isTargetValid);
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
            if (!Physics.Raycast(_camera.position, _camera.forward, out RaycastHit hit, _reach)) return;

            // ReferenceEquals compara so a referencia C#, sem a checagem nativa do == do Unity
            Collider hitCollider = hit.collider;
            if (!ReferenceEquals(hitCollider, _lastCollider))
            {
                _lastCollider = hitCollider;
                _lastColliderIsWood = hitCollider.TryGetComponent(out _lastColliderWood);
            }
            if (!_lastColliderIsWood) return;

            _target = _lastColliderWood;
            _hasTarget = true;
            _targetPlane = _target.GetCutPlane(hit.point, _camera.forward, _camera.up, _tilt);
            _isTargetValid = _target.CanCut(_targetPlane);
        }

        private void HandleAttackPressed()
        {
            if (!HasAxe || _isSwinging) return;

            // Golpe no ar tambem anima, so nao corta nada
            CuttableWood target = _hasTarget && _isTargetValid ? _target : null;
            StartCoroutine(Swing(target, _targetPlane));
        }

        private IEnumerator Swing(CuttableWood wood, Plane localPlane)
        {
            _isSwinging = true;
            Transform hand = _hand.transform;
            Quaternion rest = hand.localRotation;

            // Gira a mao em volta do eixo "direita" da camera: negativo levanta, positivo desce
            Vector3 axis = hand.parent != null ? hand.parent.InverseTransformDirection(_camera.right) : _camera.right;

            yield return RotateHand(hand, rest, axis, _windUpAngle, WindUpDuration);
            yield return RotateHand(hand, rest, axis, _strikeAngle, StrikeDuration);

            // O pedaco pode ter sido destruido enquanto o machado descia
            if (wood != null) wood.TryCut(localPlane);

            yield return RotateHand(hand, rest, axis, 0f, RecoverDuration);
            hand.localRotation = rest;
            _isSwinging = false;
        }

        private IEnumerator RotateHand(Transform hand, Quaternion rest, Vector3 axis, float angle, float duration)
        {
            Quaternion from = hand.localRotation;
            Quaternion to = Quaternion.AngleAxis(angle, axis) * rest;
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                hand.localRotation = Quaternion.Slerp(from, to, time / duration);
                yield return null;
            }
            hand.localRotation = to;
        }
    }
}
