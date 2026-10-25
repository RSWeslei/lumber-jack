using System;
using System.Collections.Generic;
using LumberJack.Core;
using UnityEngine;
using UnityEngine.InputSystem;

namespace LumberJack.Vehicles
{
    /// <summary>
    /// Um eixo do veículo: as duas rodas e se ele traciona e/ou esterca.
    /// </summary>
    [Serializable]
    public class AxleInfo
    {
        [SerializeField] private WheelCollider _leftWheel;
        [SerializeField] private WheelCollider _rightWheel;
        [Tooltip("Este eixo recebe o torque do motor")]
        [SerializeField] private bool _isMotor;
        [Tooltip("Este eixo vira com a direção")]
        [SerializeField] private bool _isSteering;

        public WheelCollider LeftWheel => _leftWheel;
        public WheelCollider RightWheel => _rightWheel;
        public bool IsMotor => _isMotor;
        public bool IsSteering => _isSteering;
    }

    /// <summary>
    /// Dirige o veículo com WheelColliders. Fica desligado até o jogador entrar pela porta.
    /// </summary>
    public class VehicleController : MonoBehaviour
    {
        private const float HandbrakeTorque = 1000f;

        [SerializeField] private List<AxleInfo> _axles = new();
        [SerializeField] private float _maxMotorTorque = 400f;
        [Tooltip("Graus")]
        [SerializeField] private float _maxSteeringAngle = 30f;
        [Tooltip("Graus por pixel de mouse ao girar a câmera em volta do carro")]
        [SerializeField] private float _cameraSensitivity = 0.1f;

        [Header("Referências")]
        [SerializeField] private Transform _playerTransform;
        [SerializeField] private Camera _vehicleCamera;

        private VehicleInputs _inputs;
        private Transform _cameraTransform;
        private bool _isHandbrakeOn;
        // Rodas visuais (primeiro filho de cada WheelCollider), cacheadas para não procurar a cada FixedUpdate
        private readonly List<(WheelCollider collider, Transform visual)> _wheelVisuals =
            new();

        private void Awake()
        {
            if (_vehicleCamera == null || _playerTransform == null)
            {
                Debug.LogError($"{nameof(VehicleController)} sem câmera ou sem referência do player.", this);
                enabled = false;
                return;
            }

            _inputs = new VehicleInputs();
            _cameraTransform = _vehicleCamera.transform;
            foreach (AxleInfo axle in _axles)
            {
                CacheWheelVisual(axle.LeftWheel);
                CacheWheelVisual(axle.RightWheel);
            }
        }

        private void OnEnable()
        {
            if (_inputs == null) return;

            _inputs.VehicleActions.Brake.performed += HandleBrakePerformed;
            _inputs.Enable();
        }

        private void Update()
        {
            Vector2 look = _inputs.VehicleMovement.Look.ReadValue<Vector2>();
            _cameraTransform.RotateAround(transform.position, Vector3.up, look.x * _cameraSensitivity);
        }

        private void FixedUpdate()
        {
            Vector2 move = _inputs.VehicleMovement.Movement.ReadValue<Vector2>();
            float motor = _maxMotorTorque * move.y;
            float steering = _maxSteeringAngle * move.x;

            foreach (AxleInfo axle in _axles)
            {
                if (axle.IsSteering)
                {
                    axle.LeftWheel.steerAngle = steering;
                    axle.RightWheel.steerAngle = steering;
                }
                if (axle.IsMotor)
                {
                    axle.LeftWheel.motorTorque = motor;
                    axle.RightWheel.motorTorque = motor;
                }
            }

            foreach ((WheelCollider wheel, Transform visual) in _wheelVisuals)
            {
                wheel.GetWorldPose(out Vector3 position, out Quaternion rotation);
                visual.SetPositionAndRotation(position, rotation);
            }
        }

        private void OnDisable()
        {
            if (_inputs == null) return;

            _inputs.VehicleActions.Brake.performed -= HandleBrakePerformed;
            _inputs.Disable();
        }

        private void OnDestroy()
        {
            _inputs?.Dispose();
        }

        public void EnterDriverSeat()
        {
            _playerTransform.gameObject.SetActive(false);
            _vehicleCamera.gameObject.SetActive(true);
            enabled = true;
        }

        private void HandleBrakePerformed(InputAction.CallbackContext context)
        {
            _isHandbrakeOn = !_isHandbrakeOn;
            float torque = _isHandbrakeOn ? HandbrakeTorque : 0f;
            foreach (AxleInfo axle in _axles)
            {
                if (!axle.IsMotor) continue;
                axle.LeftWheel.brakeTorque = torque;
                axle.RightWheel.brakeTorque = torque;
            }
        }

        private void CacheWheelVisual(WheelCollider wheel)
        {
            Transform wheelTransform = wheel.transform;
            if (wheelTransform.childCount > 0) _wheelVisuals.Add((wheel, wheelTransform.GetChild(0)));
        }
    }
}
