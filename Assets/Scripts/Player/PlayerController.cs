using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Vector3 velocity;
    private float gravity = -9.81f;
    private Vector2 move;
    private float jumpHeight = 3f;

    private CharacterController controller;
    private InputManager inputManager;
    [SerializeField] private Transform ground;

    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    private bool isGrounded;

    private void Awake() {
        controller = GetComponent<CharacterController>();
        inputManager = GetComponent<InputManager>();
    }

    private void Update() {
        Grav();
        PlayerMovement();
        Jump();
    }

    private void PlayerMovement() {
        move = inputManager.playerInputs.PlayerMovement.Movement.ReadValue<Vector2>();
        Vector3 movement = (move.y * transform.forward) + (move.x * transform.right);
        controller.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void Jump() {
        if (inputManager.playerInputs.PlayerMovement.Jump.triggered && isGrounded) {
            velocity.y = Mathf.Sqrt(jumpHeight * -2f * gravity);
        }
    }

    private void Grav() {
        isGrounded = Physics.CheckSphere(ground.position, groundDistance, groundMask);

        if (isGrounded && velocity.y < 0) {
            velocity.y = -2f;
        }

        velocity.y += gravity * Time.deltaTime;
        controller.Move(velocity * Time.deltaTime);
    }

    private void OnEnable() {
        inputManager.playerInputs.Enable();
    } 

    private void OnDisable() {
        inputManager.playerInputs.Disable();
    }
}
