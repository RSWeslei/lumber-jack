using System.Collections;
using UnityEngine;
using UnityEngine.InputSystem;

// Fica na camera: mira na madeira, mostra onde o corte vai acontecer e corta com o machado.
[RequireComponent(typeof(Camera))]
public class AxeCutter : MonoBehaviour
{
    [SerializeField] private float reach = 4f;
    [Tooltip("Inclinacao maxima do corte (Q/E ou scroll)")]
    [SerializeField] private float maxTilt = 60f;
    [SerializeField] private float tiltSpeed = 60f;

    [Header("Preview")]
    [SerializeField] private LineRenderer cutPreview;

    [Header("Machado (visual)")]
    [SerializeField] private Transform axe;
    [SerializeField] private float windUpAngle = -70f;
    [SerializeField] private float strikeAngle = 45f;

    private CutPreview _preview;
    private float _tilt;
    private bool _swinging;
    private bool _wasCursorLocked;
    private Quaternion _axeRestRotation;

    // Cache do ultimo collider mirado: evita GetComponent a cada frame no mesmo tronco
    private Collider _lastCollider;
    private CuttableWood _lastColliderWood;
    private bool _lastColliderIsWood;

    private CuttableWood _target;
    private bool _hasTarget;
    private Plane _targetPlane;
    private bool _targetValid;

    private void Awake()
    {
        if (axe != null) _axeRestRotation = axe.localRotation;
        if (cutPreview != null) _preview = new CutPreview(cutPreview);
    }

    private void Update()
    {
        HandleTilt();
        UpdateTarget();
        UpdatePreview();

        Mouse mouse = Mouse.current;
        // So corta se o cursor ja estava travado (o primeiro clique so trava o cursor)
        if (mouse != null && mouse.leftButton.wasPressedThisFrame && _wasCursorLocked && !_swinging && _hasTarget && _targetValid)
        {
            StartCoroutine(Swing(_target, _targetPlane));
        }
    }

    private void LateUpdate()
    {
        _wasCursorLocked = Cursor.lockState == CursorLockMode.Locked;
    }

    private void HandleTilt()
    {
        Keyboard keyboard = Keyboard.current;
        Mouse mouse = Mouse.current;
        if (keyboard != null)
        {
            if (keyboard.qKey.isPressed) _tilt -= tiltSpeed * Time.deltaTime;
            if (keyboard.eKey.isPressed) _tilt += tiltSpeed * Time.deltaTime;
            if (keyboard.fKey.wasPressedThisFrame) _tilt = 0f;
        }
        float scroll = mouse != null ? mouse.scroll.ReadValue().y : 0f;
        if (scroll != 0f) _tilt += Mathf.Sign(scroll) * 5f;
        _tilt = Mathf.Clamp(_tilt, -maxTilt, maxTilt);
    }

    private void UpdateTarget()
    {
        _hasTarget = false;
        _targetValid = false;

        Ray ray = new Ray(transform.position, transform.forward);
        if (!Physics.Raycast(ray, out RaycastHit hit, reach)) return;

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
        _targetValid = _target.CanCut(_targetPlane);
    }

    private void UpdatePreview()
    {
        if (_preview == null) return;

        if (!_hasTarget || _swinging) _preview.Hide();
        else _preview.Show(_target, _targetPlane, _targetValid);
    }

    private IEnumerator Swing(CuttableWood wood, Plane localPlane)
    {
        _swinging = true;

        yield return RotateAxe(windUpAngle, 0.12f);
        yield return RotateAxe(strikeAngle, 0.08f);

        // O pedaco pode ter sido destruido enquanto o machado descia
        if (wood != null) wood.TryCut(localPlane);

        yield return RotateAxe(0f, 0.25f);
        _swinging = false;
    }

    private IEnumerator RotateAxe(float angle, float duration)
    {
        if (axe == null) yield break;

        Quaternion from = axe.localRotation;
        Quaternion to = _axeRestRotation * Quaternion.Euler(angle, 0f, 0f);
        for (float time = 0f; time < duration; time += Time.deltaTime)
        {
            axe.localRotation = Quaternion.Slerp(from, to, time / duration);
            yield return null;
        }
        axe.localRotation = to;
    }

    private void OnGUI()
    {
        float size = 6f;
        Vector2 center = new Vector2(Screen.width / 2f, Screen.height / 2f);
        GUI.color = !_hasTarget ? Color.white : _targetValid ? new Color(0.2f, 1f, 0.3f) : new Color(1f, 0.25f, 0.2f);
        GUI.DrawTexture(new Rect(center.x - size / 2f, center.y - size / 2f, size, size), Texture2D.whiteTexture);

        GUI.color = Color.white;
        GUI.Label(new Rect(16, 16, 520, 140),
            "Clique esquerdo: cortar    |    Q / E ou scroll: inclinar o corte (" + Mathf.RoundToInt(_tilt) + "°)    |    F: corte reto\n" +
            "WASD: andar    |    Mouse: olhar    |    Shift: correr    |    Espaco / C: subir / descer\n" +
            "R: novo tronco    |    Esc: soltar o mouse");
    }
}
