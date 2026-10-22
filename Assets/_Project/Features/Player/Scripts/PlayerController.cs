using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private Transform _ground;
    [SerializeField] private LayerMask _groundMask;
    [SerializeField] private float _moveSpeed = 5f;
    [SerializeField] private float _groundDistance = 0.4f;
    private CharacterController _characterController;
    private Vector3 _velocity;
    private bool _isGrounded;
    private float _gravity = -9.81f;
    private float _jumpHeight = 3f;
    private const float _threshold = 0.01f;

    private float _cinemachineTargetPitch;
    public float RotationSpeed = 1.0f;
    private float _rotationVelocity;
    public float BottomClamp = -90.0f;
    public float TopClamp = 90.0f;
    public GameObject CinemachineCameraTarget;

    private void Awake() 
    {
        _characterController = GetComponent<CharacterController>();
        Cursor.lockState = CursorLockMode.Locked;
    }

    private void Update() 
    {
        Grav();
        PlayerMovement();
        Jump();
        // CameraRotation();
    }

    private void PlayerMovement() 
    {
        Vector2 move_input = InputManager.Instance.movement_input;
        Vector3 movement = (move_input.y * transform.forward) + (move_input.x * transform.right);
        _characterController.Move(movement * _moveSpeed * Time.deltaTime);
         var rot = transform.eulerAngles;
        rot.y += move_input.x * 2f;
        transform.rotation = Quaternion.Euler(rot);
    }

    private void Jump() 
    {
        if (InputManager.Instance.jump_input && _isGrounded) {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
        }
    }

    private static float ClampAngle(float lfAngle, float lfMin, float lfMax)
    {
        if (lfAngle < -360f) lfAngle += 360f;
        if (lfAngle > 360f) lfAngle -= 360f;
        return Mathf.Clamp(lfAngle, lfMin, lfMax);
    }

    private void CameraRotation()
    {
        // if there is an input
        if (InputManager.Instance.look_input.sqrMagnitude >= _threshold)
        {
            //Don't multiply mouse input by Time.deltaTime
            float deltaTimeMultiplier = true ? 1.0f : Time.deltaTime;
            
            _cinemachineTargetPitch += InputManager.Instance.look_input.y * RotationSpeed * deltaTimeMultiplier;
            _rotationVelocity = InputManager.Instance.look_input.x * RotationSpeed * deltaTimeMultiplier;

            // clamp our pitch rotation
            _cinemachineTargetPitch = ClampAngle(_cinemachineTargetPitch, BottomClamp, TopClamp);

            // Update Cinemachine camera target pitch
            CinemachineCameraTarget.transform.localRotation = Quaternion.Euler(_cinemachineTargetPitch, 0.0f, 0.0f);

            // rotate the player left and right
            transform.Rotate(Vector3.up * _rotationVelocity);
        }
    }

    private void Grav() 
    {
        _isGrounded = Physics.CheckSphere(_ground.position, _groundDistance, _groundMask);

        if (_isGrounded && _velocity.y < 0) 
        {
            _velocity.y = -2f;
        }
        _velocity.y += _gravity * Time.deltaTime;
        _characterController.Move(_velocity * Time.deltaTime);
    }

    private void OnEnable() 
    {
        InputManager.Instance.playerInputs.Enable();
    } 

    private void OnDisable() 
    {
        InputManager.Instance.playerInputs.Disable();
    }
}
