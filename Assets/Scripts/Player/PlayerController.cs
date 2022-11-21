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

    private void Awake() 
    {
        _characterController = GetComponent<CharacterController>();
    }

    private void Update() 
    {
        Grav();
        PlayerMovement();
        Jump();
    }

    private void PlayerMovement() 
    {
        Vector2 move_input = InputManager.Instance.movement_input;
        Vector3 movement = (move_input.y * transform.forward) + (move_input.x * transform.right);
        _characterController.Move(movement * _moveSpeed * Time.deltaTime);
    }

    private void Jump() 
    {
        if (InputManager.Instance.jump_input && _isGrounded) {
            _velocity.y = Mathf.Sqrt(_jumpHeight * -2f * _gravity);
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
