using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Agarra rigidbodies com a tecla de pegar e leva o ponto agarrado para a frente da mira
    /// pela física (colide normalmente). Objetos pesados vêm mais devagar. Solta ao largar a
    /// tecla ou se o objeto ficar preso longe demais.
    /// </summary>
    public class PickupObjects : MonoBehaviour
    {
        private const float MinHoldDistance = 1f;

        [Tooltip("Camada(s) que podem ser agarradas")]
        [SerializeField] private LayerMask _layerMask;
        [Tooltip("Câmera do jogador (com o PlayerAim)")]
        [SerializeField] private Transform _cameraTransform;

        [Header("Carregar")]
        [Tooltip("Solta o objeto se o ponto agarrado ficar mais longe que isso (m) de onde deveria estar")]
        [SerializeField] private float _maxDistance = 1.5f;
        [Tooltip("Quanto mais alto, mais rápido o objeto alcança a mira")]
        [SerializeField] private float _followStrength = 12f;
        [Tooltip("Velocidade máxima (m/s) de um objeto leve")]
        [SerializeField] private float _maxCarrySpeed = 8f;
        [Tooltip("Até esta massa (kg) o objeto conta como leve; acima disso fica proporcionalmente mais lento")]
        [SerializeField] private float _referenceMass = 50f;

        private PlayerAim _aim;
        private Rigidbody _carriedBody;
        private bool _isCarrying;
        private Vector3 _localGrabPoint;
        private float _holdDistance;
        private bool _previousUseGravity;
        private bool _previousFreezeRotation;

        private void Awake()
        {
            _aim = _cameraTransform.GetComponent<PlayerAim>();
        }

        private void OnEnable()
        {
            InputManager.Instance.GrabPressed += HandleGrabPressed;
            InputManager.Instance.GrabReleased += HandleGrabReleased;
        }

        private void FixedUpdate()
        {
            if (!_isCarrying) return;

            // O objeto pode ter sido destruído enquanto era carregado (vendido, refinado, cortado)
            if (!_carriedBody)
            {
                ClearCarried();
                return;
            }

            Vector3 target = _cameraTransform.position + _cameraTransform.forward * _holdDistance;
            Vector3 grabPoint = _carriedBody.transform.TransformPoint(_localGrabPoint);
            Vector3 toTarget = target - grabPoint;
            float distance = toTarget.magnitude;

            if (distance > _maxDistance)
            {
                Drop();
                return;
            }

            float heaviness = Mathf.Max(1f, _carriedBody.mass / _referenceMass);
            float speed = Mathf.Min(distance * _followStrength, _maxCarrySpeed / heaviness);
            _carriedBody.linearVelocity = distance > 0f ? toTarget / distance * speed : Vector3.zero;
        }

        private void OnDisable()
        {
            InputManager.Instance.GrabPressed -= HandleGrabPressed;
            InputManager.Instance.GrabReleased -= HandleGrabReleased;
            Drop();
        }

        private void HandleGrabPressed()
        {
            if (_isCarrying || !_aim.TryFindTarget(_layerMask, out RaycastHit hit)) return;

            Rigidbody body = hit.rigidbody;
            if (body == null || body.isKinematic) return;

            _carriedBody = body;
            _isCarrying = true;
            _localGrabPoint = body.transform.InverseTransformPoint(hit.point);
            _holdDistance = Mathf.Clamp(hit.distance, MinHoldDistance, _aim.Reach);

            _previousUseGravity = body.useGravity;
            _previousFreezeRotation = body.freezeRotation;
            body.useGravity = false;
            // Sem girar enquanto carrega: o ponto agarrado fica sempre no mesmo lugar do objeto
            body.freezeRotation = true;
        }

        private void HandleGrabReleased()
        {
            Drop();
        }

        private void Drop()
        {
            if (!_isCarrying) return;

            if (_carriedBody)
            {
                _carriedBody.useGravity = _previousUseGravity;
                _carriedBody.freezeRotation = _previousFreezeRotation;
            }
            ClearCarried();
        }

        private void ClearCarried()
        {
            _carriedBody = null;
            _isCarrying = false;
        }
    }
}
