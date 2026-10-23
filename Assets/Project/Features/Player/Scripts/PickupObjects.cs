using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Agarra rigidbodies com a tecla de pegar e solta ao largar a tecla ou se afastar demais.
    /// </summary>
    public class PickupObjects : MonoBehaviour
    {
        [Header("Carregar")]
        [SerializeField] private Transform _carryPosition;
        [Tooltip("Solta o objeto se ele ficar mais longe que isso do ponto de carregar")]
        [SerializeField] private float _maxDistance = 1.5f;

        [Header("Raycast")]
        [SerializeField] private float _raycastDistance = 2f;
        [SerializeField] private LayerMask _layerMask;
        [SerializeField] private Transform _cameraTransform;

        private Rigidbody _carriedBody;
        // Flag propria: evita a checagem de null do Unity a cada frame no Update
        private bool _isCarrying;

        public bool IsCarrying => _isCarrying;

        private void OnEnable()
        {
            InputManager.Instance.GrabPressed += HandleGrabPressed;
            InputManager.Instance.GrabReleased += HandleGrabReleased;
        }

        private void Update()
        {
            if (_isCarrying) CheckCarryDistance();
        }

        private void OnDisable()
        {
            InputManager.Instance.GrabPressed -= HandleGrabPressed;
            InputManager.Instance.GrabReleased -= HandleGrabReleased;
        }

        private void HandleGrabPressed()
        {
            if (!Physics.Raycast(_cameraTransform.position, _cameraTransform.forward, out RaycastHit hit, _raycastDistance, _layerMask)) return;

            if (!hit.transform.TryGetComponent(out Rigidbody body)) return;

            _carriedBody = body;
            _carriedBody.useGravity = false;
            _carriedBody.freezeRotation = true;
            _isCarrying = true;
        }

        private void HandleGrabReleased()
        {
            Drop();
        }

        private void CheckCarryDistance()
        {
            // O objeto pode ter sido destruido enquanto era carregado (ex.: vendido, cortado)
            if (!_carriedBody)
            {
                ClearCarried();
                return;
            }

            float distance = Vector3.Distance(_carriedBody.position, _carryPosition.position);
            if (distance > _maxDistance) Drop();
        }

        private void Drop()
        {
            if (!_isCarrying) return;

            if (_carriedBody)
            {
                _carriedBody.useGravity = true;
                _carriedBody.freezeRotation = false;
                _carriedBody.transform.SetParent(null);
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
