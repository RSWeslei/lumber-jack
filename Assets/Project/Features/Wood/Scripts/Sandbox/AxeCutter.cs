using System;
using System.Threading;
using LumberJack.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Wood
{
    /// <summary>
    /// Só para a cena de teste: fica na câmera, mira na madeira, mostra onde o corte vai
    /// acontecer e corta com o machado. Lê o teclado direto porque a cena não tem InputManager.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class AxeCutter : MonoBehaviour
    {
        private const float ScrollTiltStep = 5f;
        private const float WindUpDuration = 0.12f;
        private const float StrikeDuration = 0.08f;
        private const float RecoverDuration = 0.25f;
        private const float CrosshairSize = 6f;
        private const string WoodLayerName = "Wood";
        private const string CutBinding = "<Mouse>/leftButton";

        private static readonly Color ValidColor = new(0.2f, 1f, 0.3f);
        private static readonly Color InvalidColor = new(1f, 0.25f, 0.2f);
        private static readonly Rect HelpRect = new(16, 16, 520, 140);

        [SerializeField] private float _reach = 4f;
        [Tooltip("Inclinação máxima do corte (Q/E ou scroll)")]
        [SerializeField] private float _maxTilt = 60f;
        [Tooltip("Graus por segundo segurando Q/E")]
        [SerializeField] private float _tiltSpeed = 60f;
        [Tooltip("Dano por golpe (como o machado do jogador)")]
        [SerializeField] private float _damage = 1f;

        [Header("Preview")]
        [SerializeField] private LineRenderer _cutPreview;

        [Header("Machado (visual)")]
        [SerializeField] private Transform _axe;
        [SerializeField] private float _windUpAngle = -70f;
        [SerializeField] private float _strikeAngle = 45f;

        private CutPreview _preview;
        private bool _hasPreview;
        private bool _hasAxe;
        private float _tilt;
        private bool _isSwinging;
        private bool _wasCursorLocked;
        private Quaternion _axeRestRotation;
        private int _woodMask;
        // A cena de teste não tem InputManager: ação propria só para o clique de cortar
        private InputAction _cutAction;

        // Cache do último collider mirado: evita GetComponent a cada frame no mesmo tronco
        private Collider _lastCollider;
        private IAxeTarget _lastColliderTarget;
        private bool _lastColliderIsWood;

        private IAxeTarget _target;
        private bool _hasTarget;
        private Plane _targetPlane;
        private bool _isTargetValid;

        // Texto de ajuda remontado só quando o ângulo mostrado muda (OnGUI roda várias vezes por frame)
        private string _helpText;
        private int _helpTextTilt = int.MinValue;

        private void Awake()
        {
            _hasAxe = _axe != null;
            if (_hasAxe) _axeRestRotation = _axe.localRotation;

            _hasPreview = _cutPreview != null;
            if (_hasPreview) _preview = new CutPreview(_cutPreview);

            _woodMask = LayerMask.GetMask(WoodLayerName);
            _cutAction = new InputAction("Cut", InputActionType.Button, CutBinding);
        }

        private void OnEnable()
        {
            _cutAction.performed += HandleCutPerformed;
            _cutAction.Enable();
        }

        private void Update()
        {
            UpdateTilt();
            UpdateTarget();
            UpdatePreview();
        }

        private void LateUpdate()
        {
            _wasCursorLocked = Cursor.lockState == CursorLockMode.Locked;
        }

        private void OnGUI()
        {
            Vector2 center = new(Screen.width / 2f, Screen.height / 2f);
            GUI.color = !_hasTarget ? Color.white : _isTargetValid ? ValidColor : InvalidColor;
            GUI.DrawTexture(new Rect(center.x - CrosshairSize / 2f, center.y - CrosshairSize / 2f, CrosshairSize, CrosshairSize),
                Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(HelpRect, GetHelpText());
        }

        private void OnDisable()
        {
            _cutAction.performed -= HandleCutPerformed;
            _cutAction.Disable();
        }

        private void OnDestroy()
        {
            _cutAction.Dispose();
        }

        private string GetHelpText()
        {
            int tilt = Mathf.RoundToInt(_tilt);
            if (tilt == _helpTextTilt) return _helpText;

            _helpTextTilt = tilt;
            _helpText =
                $"Clique esquerdo: cortar    |    Q / E ou scroll: inclinar o corte ({tilt}°)    |    F: corte reto\n" +
                "WASD: andar    |    Mouse: olhar    |    Shift: correr    |    Espaço / C: subir / descer\n" +
                "R: novo tronco    |    Esc: soltar o mouse";
            return _helpText;
        }

        private void UpdateTilt()
        {
            Keyboard keyboard = Keyboard.current;
            Mouse mouse = Mouse.current;
            if (keyboard != null)
            {
                if (keyboard.qKey.isPressed) _tilt -= _tiltSpeed * Time.deltaTime;
                if (keyboard.eKey.isPressed) _tilt += _tiltSpeed * Time.deltaTime;
                if (keyboard.fKey.wasPressedThisFrame) _tilt = 0f;
            }
            float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
            if (scroll != 0f) _tilt += Mathf.Sign(scroll) * ScrollTiltStep;
            _tilt = Mathf.Clamp(_tilt, -_maxTilt, _maxTilt);
        }

        private void UpdateTarget()
        {
            _hasTarget = false;
            _isTargetValid = false;

            if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _reach, _woodMask,
                    QueryTriggerInteraction.Ignore)) return;

            // ReferenceEquals compara só a referência C#, sem a checagem nativa do == do Unity
            Collider hitCollider = hit.collider;
            if (!ReferenceEquals(hitCollider, _lastCollider))
            {
                _lastCollider = hitCollider;
                // Os pedaços têm os colisores em filhos: o alvo fica no corpo
                Rigidbody body = hitCollider.attachedRigidbody;
                _lastColliderIsWood = !ReferenceEquals(body, null) && body.TryGetComponent(out _lastColliderTarget);
            }
            if (!_lastColliderIsWood) return;

            _target = _lastColliderTarget;
            _hasTarget = true;
            _targetPlane = _target.GetCutPlane(hit.point, transform.forward, transform.up, _tilt);
            _isTargetValid = _target.CanCut(_targetPlane);
        }

        private void UpdatePreview()
        {
            if (!_hasPreview) return;

            if (!_hasTarget || _isSwinging) _preview.Hide();
            else _preview.Show(_target, _targetPlane, _isTargetValid, _target.CutProgress);
        }

        private async void HandleCutPerformed(InputAction.CallbackContext context)
        {
            // Só corta se o cursor já estava travado (o primeiro clique só trava o cursor)
            if (!_wasCursorLocked || _isSwinging || !_hasTarget || !_isTargetValid) return;

            try
            {
                await SwingAsync(_target, _targetPlane, destroyCancellationToken);
            }
            catch (OperationCanceledException)
            {
                // A câmera foi destruída no meio do golpe (saiu do Play)
            }
        }

        private async Awaitable SwingAsync(IAxeTarget target, Plane localPlane, CancellationToken token)
        {
            _isSwinging = true;

            await RotateAxeAsync(_windUpAngle, WindUpDuration, token);
            await RotateAxeAsync(_strikeAngle, StrikeDuration, token);

            // O pedaço pode ter sido destruído enquanto o machado descia
            if (target is UnityEngine.Object targetObject && targetObject != null) target.Strike(localPlane, transform.forward, _damage);

            await RotateAxeAsync(0f, RecoverDuration, token);
            _isSwinging = false;
        }

        private async Awaitable RotateAxeAsync(float angle, float duration, CancellationToken token)
        {
            if (!_hasAxe) return;

            Quaternion from = _axe.localRotation;
            Quaternion to = _axeRestRotation * Quaternion.Euler(angle, 0f, 0f);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                _axe.localRotation = Quaternion.Slerp(from, to, time / duration);
                await Awaitable.NextFrameAsync(token);
            }
            _axe.localRotation = to;
        }
    }
}
