using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Wood
{
    /// <summary>
    /// So para a cena de teste: fica na camera, mira na madeira, mostra onde o corte vai
    /// acontecer e corta com o machado. Le o teclado direto porque a cena nao tem InputManager.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    public class AxeCutter : MonoBehaviour
    {
        private const float ScrollTiltStep = 5f;
        private const float WindUpDuration = 0.12f;
        private const float StrikeDuration = 0.08f;
        private const float RecoverDuration = 0.25f;
        private const float CrosshairSize = 6f;

        private static readonly Color ValidColor = new Color(0.2f, 1f, 0.3f);
        private static readonly Color InvalidColor = new Color(1f, 0.25f, 0.2f);
        private static readonly Rect HelpRect = new Rect(16, 16, 520, 140);

        [SerializeField] private float _reach = 4f;
        [Tooltip("Inclinacao maxima do corte (Q/E ou scroll)")]
        [SerializeField] private float _maxTilt = 60f;
        [Tooltip("Graus por segundo segurando Q/E")]
        [SerializeField] private float _tiltSpeed = 60f;

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

        // Cache do ultimo collider mirado: evita GetComponent a cada frame no mesmo tronco
        private Collider _lastCollider;
        private CuttableWood _lastColliderWood;
        private bool _lastColliderIsWood;

        private CuttableWood _target;
        private bool _hasTarget;
        private Plane _targetPlane;
        private bool _isTargetValid;

        // Texto de ajuda remontado so quando o angulo mostrado muda (OnGUI roda varias vezes por frame)
        private string _helpText;
        private int _helpTextTilt = int.MinValue;

        private void Awake()
        {
            _hasAxe = _axe != null;
            if (_hasAxe) _axeRestRotation = _axe.localRotation;

            _hasPreview = _cutPreview != null;
            if (_hasPreview) _preview = new CutPreview(_cutPreview);
        }

        private void Update()
        {
            UpdateTilt();
            UpdateTarget();
            UpdatePreview();

            Mouse mouse = Mouse.current;
            // So corta se o cursor ja estava travado (o primeiro clique so trava o cursor)
            bool clicked = mouse != null && mouse.leftButton.wasPressedThisFrame;
            if (clicked && _wasCursorLocked && !_isSwinging && _hasTarget && _isTargetValid)
            {
                StartCoroutine(Swing(_target, _targetPlane));
            }
        }

        private void LateUpdate()
        {
            _wasCursorLocked = Cursor.lockState == CursorLockMode.Locked;
        }

        private void OnGUI()
        {
            Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
            GUI.color = !_hasTarget ? Color.white : _isTargetValid ? ValidColor : InvalidColor;
            GUI.DrawTexture(new Rect(center.x - CrosshairSize / 2f, center.y - CrosshairSize / 2f, CrosshairSize, CrosshairSize),
                Texture2D.whiteTexture);

            GUI.color = Color.white;
            GUI.Label(HelpRect, GetHelpText());
        }

        private string GetHelpText()
        {
            int tilt = Mathf.RoundToInt(_tilt);
            if (tilt == _helpTextTilt) return _helpText;

            _helpTextTilt = tilt;
            _helpText =
                $"Clique esquerdo: cortar    |    Q / E ou scroll: inclinar o corte ({tilt}°)    |    F: corte reto\n" +
                "WASD: andar    |    Mouse: olhar    |    Shift: correr    |    Espaco / C: subir / descer\n" +
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

            if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _reach)) return;

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
            _targetPlane = _target.GetCutPlane(hit.point, transform.forward, transform.up, _tilt);
            _isTargetValid = _target.CanCut(_targetPlane);
        }

        private void UpdatePreview()
        {
            if (!_hasPreview) return;

            if (!_hasTarget || _isSwinging) _preview.Hide();
            else _preview.Show(_target, _targetPlane, _isTargetValid);
        }

        private IEnumerator Swing(CuttableWood wood, Plane localPlane)
        {
            _isSwinging = true;

            yield return RotateAxe(_windUpAngle, WindUpDuration);
            yield return RotateAxe(_strikeAngle, StrikeDuration);

            // O pedaco pode ter sido destruido enquanto o machado descia
            if (wood != null) wood.TryCut(localPlane);

            yield return RotateAxe(0f, RecoverDuration);
            _isSwinging = false;
        }

        private IEnumerator RotateAxe(float angle, float duration)
        {
            if (!_hasAxe) yield break;

            Quaternion from = _axe.localRotation;
            Quaternion to = _axeRestRotation * Quaternion.Euler(angle, 0f, 0f);
            for (float time = 0f; time < duration; time += Time.deltaTime)
            {
                _axe.localRotation = Quaternion.Slerp(from, to, time / duration);
                yield return null;
            }
            _axe.localRotation = to;
        }
    }
}
