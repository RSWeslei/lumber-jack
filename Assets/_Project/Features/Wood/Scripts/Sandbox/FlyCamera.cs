using UnityEngine;
using UnityEngine.InputSystem;

// Camera simples para a cena de teste: WASD + mouse, sem precisar do player inteiro.
public class FlyCamera : MonoBehaviour
{
    [SerializeField] private float moveSpeed = 3f;
    [SerializeField] private float sprintMultiplier = 2.5f;
    [SerializeField] private float lookSensitivity = 0.1f;

    private float _yaw;
    private float _pitch;

    private void Start()
    {
        Vector3 euler = transform.eulerAngles;
        _yaw = euler.y;
        _pitch = euler.x > 180f ? euler.x - 360f : euler.x;
        LockCursor(true);
    }

    private void Update()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard == null || mouse == null) return;

        if (keyboard.escapeKey.wasPressedThisFrame) LockCursor(false);
        if (mouse.leftButton.wasPressedThisFrame && Cursor.lockState != CursorLockMode.Locked) LockCursor(true);
        if (Cursor.lockState != CursorLockMode.Locked) return;

        Vector2 look = mouse.delta.ReadValue() * lookSensitivity;
        _yaw += look.x;
        _pitch = Mathf.Clamp(_pitch - look.y, -89f, 89f);
        transform.rotation = Quaternion.Euler(_pitch, _yaw, 0f);

        // Anda no plano horizontal, sobe/desce com Espaco/C
        Vector3 forward = Vector3.ProjectOnPlane(transform.forward, Vector3.up).normalized;
        Vector3 right = Vector3.ProjectOnPlane(transform.right, Vector3.up).normalized;
        Vector3 move = Vector3.zero;
        if (keyboard.wKey.isPressed) move += forward;
        if (keyboard.sKey.isPressed) move -= forward;
        if (keyboard.dKey.isPressed) move += right;
        if (keyboard.aKey.isPressed) move -= right;
        if (keyboard.spaceKey.isPressed) move += Vector3.up;
        if (keyboard.cKey.isPressed) move -= Vector3.up;

        float speed = moveSpeed * (keyboard.leftShiftKey.isPressed ? sprintMultiplier : 1f);
        transform.position += move.normalized * speed * Time.deltaTime;
    }

    private void LockCursor(bool locked)
    {
        Cursor.lockState = locked ? CursorLockMode.Locked : CursorLockMode.None;
        Cursor.visible = !locked;
    }
}
