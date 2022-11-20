using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Managers;

public class PlayerController : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 5f;
    private Vector3 velocity;
    private float gravity = -9.81f;
    private float jumpHeight = 3f;

    private CharacterController controller;
    [SerializeField] private Transform ground;

    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    private bool isGrounded;

    private void Awake() {
        controller = GetComponent<CharacterController>();
    }

    private void Update() {
        Grav();
        PlayerMovement();
        Jump();
    }

    private void PlayerMovement() {
        Vector2 move_input = InputManager.Instance.movement_input;
        Vector3 movement = (move_input.y * transform.forward) + (move_input.x * transform.right);
        controller.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void Jump() {
        if (InputManager.Instance.jump_input && isGrounded) {
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
        InputManager.Instance.playerInputs.Enable();
    } 

    private void OnDisable() {
        InputManager.Instance.playerInputs.Disable();
    }
}
