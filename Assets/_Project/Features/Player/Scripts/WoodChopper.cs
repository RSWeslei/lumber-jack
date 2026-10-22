using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Corta madeira com o machado equipado na hotbar.
// Mira na madeira, mostra a linha do corte e corta no clique (OnPlayerAttack).
public class WoodChopper : MonoBehaviour
{
    [SerializeField] private Hand _hand;
    [SerializeField] private Material _previewMaterial;
    [SerializeField] private float _reach = 2.5f;

    [Header("Inclinacao do corte (Z / X, C volta para reto)")]
    [SerializeField] private float _maxTilt = 60f;
    [SerializeField] private float _tiltSpeed = 60f;

    [Header("Golpe")]
    [SerializeField] private float _windUpAngle = -60f;
    [SerializeField] private float _strikeAngle = 40f;

    private Transform _camera;
    private CutPreview _preview;
    private float _tilt;
    private bool _swinging;

    private CuttableWood _target;
    private Plane _targetPlane;
    private bool _targetValid;

    private bool HasAxe => _hand != null && _hand.EquippedItem is ToolSO tool && tool.canCutWood;

    private void Awake()
    {
        Camera parentCamera = GetComponentInParent<Camera>();
        _camera = parentCamera != null ? parentCamera.transform : Camera.main.transform;
        _preview = CutPreview.Create(transform, _previewMaterial);
    }

    private void OnEnable()
    {
        InputManager.Instance.OnPlayerAttack += Chop;
    }

    private void OnDisable()
    {
        InputManager.Instance.OnPlayerAttack -= Chop;
    }

    private void Update()
    {
        _target = null;
        if (!HasAxe || _swinging)
        {
            _preview.Hide();
            return;
        }

        HandleTilt();
        UpdateTarget();

        if (_target != null) _preview.Show(_target, _targetPlane, _targetValid);
        else _preview.Hide();
    }

    private void HandleTilt()
    {
        Keyboard keyboard = Keyboard.current;
        if (keyboard == null) return;

        if (keyboard.zKey.isPressed) _tilt -= _tiltSpeed * Time.deltaTime;
        if (keyboard.xKey.isPressed) _tilt += _tiltSpeed * Time.deltaTime;
        if (keyboard.cKey.wasPressedThisFrame) _tilt = 0f;
        _tilt = Mathf.Clamp(_tilt, -_maxTilt, _maxTilt);
    }

    private void UpdateTarget()
    {
        if (!Physics.Raycast(_camera.position, _camera.forward, out RaycastHit hit, _reach)) return;

        CuttableWood wood = hit.collider.GetComponent<CuttableWood>();
        if (wood == null) return;

        _target = wood;
        _targetPlane = wood.GetCutPlane(hit.point, _camera.forward, _camera.up, _tilt);
        _targetValid = wood.CanCut(_targetPlane);
    }

    private void Chop()
    {
        if (!HasAxe || _swinging) return;

        // Golpe no ar tambem anima, so nao corta nada
        CuttableWood target = _targetValid ? _target : null;
        StartCoroutine(Swing(target, _targetPlane));
    }

    private IEnumerator Swing(CuttableWood wood, Plane localPlane)
    {
        _swinging = true;
        Transform hand = _hand.transform;
        Quaternion rest = hand.localRotation;

        // Gira a mao em volta do eixo "direita" da camera: negativo levanta, positivo desce
        Vector3 axis = hand.parent != null ? hand.parent.InverseTransformDirection(_camera.right) : _camera.right;

        yield return RotateHand(hand, rest, axis, _windUpAngle, 0.15f);
        yield return RotateHand(hand, rest, axis, _strikeAngle, 0.08f);

        // O pedaco pode ter sido destruido enquanto o machado descia
        if (wood != null) wood.TryCut(localPlane);

        yield return RotateHand(hand, rest, axis, 0f, 0.25f);
        hand.localRotation = rest;
        _swinging = false;
    }

    private IEnumerator RotateHand(Transform hand, Quaternion rest, Vector3 axis, float angle, float duration)
    {
        Quaternion from = hand.localRotation;
        Quaternion to = Quaternion.AngleAxis(angle, axis) * rest;
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            hand.localRotation = Quaternion.Slerp(from, to, time / duration);
            yield return null;
        }
        hand.localRotation = to;
    }
}
