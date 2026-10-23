using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Player
{
    // Camera em primeira pessoa: mouse cru (sem suavizacao/atraso), balanco da cabeca,
    // "afundada" ao aterrissar, leve inclinacao ao andar de lado e FOV maior ao correr.
    // Roda antes do PlayerMovement para o movimento usar a rotacao deste frame.
    [DefaultExecutionOrder(-10)]
    [RequireComponent(typeof(PlayerMovement))]
    public class FirstPersonCamera : MonoBehaviour
    {
        [Tooltip("Pivo da cabeca (altura dos olhos); gira no eixo X")]
        [SerializeField] private Transform _cameraRoot;
        [Tooltip("Vazio = usa a Camera.main")]
        [SerializeField] private Camera _camera;

        [Header("Olhar")]
        [SerializeField] private float _mouseSensitivity = 1f;
        [SerializeField] private float _gamepadSensitivity = 1f;
        [SerializeField] private float _pitchLimit = 89f;
        [Tooltip("Espalha cada leitura do mouse por essa janela (s). Mouses de 125 Hz deixam frames sem " +
                 "movimento e a camera gira aos trancos; ~0.01 resolve com atraso minimo. 0 = mouse cru")]
        [SerializeField] private float _mouseResampleWindow = 0.01f;

        [Header("Lente")]
        [SerializeField] private float _baseFov = 70f;
        [SerializeField] private float _sprintFovBoost = 8f;
        [SerializeField] private float _fovSharpness = 8f;
        [SerializeField] private float _nearClip = 0.05f;

        [Header("Balanco da cabeca")]
        [Tooltip("Passos por metro andado")]
        [SerializeField] private float _bobFrequency = 0.55f;
        [SerializeField] private float _bobVerticalAmplitude = 0.04f;
        [SerializeField] private float _bobHorizontalAmplitude = 0.025f;
        [SerializeField] private float _bobSharpness = 10f;

        [Header("Aterrissagem")]
        [Tooltip("Quanto a camera desce por m/s de queda")]
        [SerializeField] private float _landingImpulse = 0.22f;
        [SerializeField] private float _maxLandingImpulse = 2.2f;
        [SerializeField] private float _springStiffness = 120f;
        [SerializeField] private float _springDamping = 16f;

        [Header("Inclinacao ao andar de lado")]
        [SerializeField] private float _strafeTilt = 1.2f;
        [SerializeField] private float _tiltSharpness = 8f;

        private PlayerMovement _movement;
        private Transform _cameraTransform;
        private readonly Queue<(float time, Vector2 delta)> _mouseSamples = new Queue<(float, Vector2)>();
        private float _pitch;
        private float _bobPhase;
        private float _bobWeight;
        private float _landingOffset;
        private float _landingVelocity;
        private float _tilt;

        private void Awake()
        {
            _movement = GetComponent<PlayerMovement>();
            if (_camera == null) _camera = Camera.main;
            _cameraTransform = _camera.transform;
            _camera.nearClipPlane = _nearClip;
            _camera.fieldOfView = _baseFov;
            _pitch = NormalizeAngle(_cameraRoot.localEulerAngles.x);
        }

        private void OnEnable()
        {
            _movement.OnLanded += HandleLanded;
            LockCursor();
        }

        private void OnDisable()
        {
            _movement.OnLanded -= HandleLanded;
        }

        private void OnApplicationFocus(bool hasFocus)
        {
            if (hasFocus && enabled) LockCursor();
        }

        private void Update()
        {
            Look();
        }

        private void LateUpdate()
        {
            float deltaTime = Time.deltaTime;
            Vector3 bob = UpdateHeadBob(deltaTime);
            UpdateLandingSpring(deltaTime);
            UpdateTilt(deltaTime);
            UpdateFov(deltaTime);

            // A camera acompanha a cabeca exatamente, mais os efeitos
            _cameraTransform.SetPositionAndRotation(
                _cameraRoot.position + _cameraRoot.rotation * bob + Vector3.up * _landingOffset,
                _cameraRoot.rotation * Quaternion.Euler(0f, 0f, _tilt));
        }

        private void Look()
        {
            if (Cursor.lockState != CursorLockMode.Locked) return;

            InputAction look = InputManager.Instance.PlayerInputs.PlayerMovement.Look;
            Vector2 delta = look.ReadValue<Vector2>();

            // Mouse ja vem em "pixels deste frame"; o analogico e uma velocidade, entao usa deltaTime
            bool gamepad = look.activeControl != null && look.activeControl.device is Gamepad;
            if (gamepad) delta *= _gamepadSensitivity * Time.deltaTime;
            else delta = ResampleMouse(delta) * _mouseSensitivity;

            _pitch = Mathf.Clamp(_pitch + delta.y, -_pitchLimit, _pitchLimit);
            _cameraRoot.localRotation = Quaternion.Euler(_pitch, 0f, 0f);
            transform.Rotate(Vector3.up * delta.x);
        }

        // Media movel da *velocidade* do mouse numa janela curta: cada leitura vira um giro continuo
        // durante a janela, e o total girado continua igual ao do mouse (nada se perde nem sobra).
        private Vector2 ResampleMouse(Vector2 rawDelta)
        {
            if (_mouseResampleWindow <= 0f) return rawDelta;

            // Com FPS baixo (frame maior que a janela) o resultado e o proprio delta, sem atraso
            float deltaTime = Time.unscaledDeltaTime;
            float window = Mathf.Max(_mouseResampleWindow, deltaTime);

            float now = Time.unscaledTime;
            _mouseSamples.Enqueue((now, rawDelta));
            while (_mouseSamples.Count > 0 && _mouseSamples.Peek().time <= now - window) _mouseSamples.Dequeue();

            Vector2 sum = Vector2.zero;
            foreach ((float _, Vector2 sample) in _mouseSamples) sum += sample;
            return sum * (deltaTime / window);
        }

        private Vector3 UpdateHeadBob(float deltaTime)
        {
            float speed = _movement.IsGrounded ? _movement.HorizontalSpeed : 0f;
            float targetWeight = Mathf.Clamp01(speed / _movement.WalkSpeed);
            _bobWeight = Mathf.Lerp(_bobWeight, targetWeight, Sharpness(_bobSharpness, deltaTime));

            // A fase avanca com a distancia andada, entao correr da passos mais rapidos naturalmente
            _bobPhase += speed * _bobFrequency * Mathf.PI * deltaTime;

            return new Vector3(
                Mathf.Sin(_bobPhase) * _bobHorizontalAmplitude,
                Mathf.Sin(_bobPhase * 2f) * _bobVerticalAmplitude,
                0f) * _bobWeight;
        }

        private void HandleLanded(float fallSpeed)
        {
            _landingVelocity -= Mathf.Min(fallSpeed * _landingImpulse, _maxLandingImpulse);
        }

        // Mola amortecida: afunda e volta suave depois da aterrissagem
        private void UpdateLandingSpring(float deltaTime)
        {
            float acceleration = -_springStiffness * _landingOffset - _springDamping * _landingVelocity;
            _landingVelocity += acceleration * deltaTime;
            _landingOffset += _landingVelocity * deltaTime;
        }

        private void UpdateTilt(float deltaTime)
        {
            float target = -_movement.MoveInput.x * _strafeTilt;
            _tilt = Mathf.Lerp(_tilt, target, Sharpness(_tiltSharpness, deltaTime));
        }

        private void UpdateFov(float deltaTime)
        {
            bool sprinting = _movement.IsSprinting && _movement.HorizontalSpeed > _movement.WalkSpeed + 0.5f;
            float target = _baseFov + (sprinting ? _sprintFovBoost : 0f);
            _camera.fieldOfView = Mathf.Lerp(_camera.fieldOfView, target, Sharpness(_fovSharpness, deltaTime));
        }

        private static void LockCursor()
        {
            Cursor.lockState = CursorLockMode.Locked;
            Cursor.visible = false;
        }

        // Suavizacao independente do framerate
        private static float Sharpness(float sharpness, float deltaTime)
        {
            return 1f - Mathf.Exp(-sharpness * deltaTime);
        }

        private static float NormalizeAngle(float angle)
        {
            return angle > 180f ? angle - 360f : angle;
        }
    }
}
