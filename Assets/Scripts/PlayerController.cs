using System.Collections;
using System.Collections.Generic;
using UnityEngine;

public class PlayerController : MonoBehaviour
{
    private InputSystemFirstPersonControls controls;
    [SerializeField] private float moveSpeed = 5f;
    private Vector3 velocity;
    private float gravity = -9.81f;
    private Vector2 move;
    private float jumpHeight = 3f;

    private CharacterController controller;
    [SerializeField] private Transform ground;

    [SerializeField] float groundDistance = 0.4f;
    [SerializeField] private LayerMask groundMask;
    private bool isGrounded;

    private void Awake() {
        controller = GetComponent<CharacterController>();
        controls = new InputSystemFirstPersonControls();
    }

    private void Update() {
        Grav();
        PlayerMovement();
        Jump();
    }

    private void PlayerMovement() {
        move = controls.Player.Movement.ReadValue<Vector2>();
        Vector3 movement = (move.y * transform.forward) + (move.x * transform.right);
        controller.Move(movement * moveSpeed * Time.deltaTime);
    }

    private void Jump() {
        if (controls.Player.Jump.triggered && isGrounded) {
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
        controls.Enable();
    } 

    private void OnDisable() {
        controls.Disable();
    }
}