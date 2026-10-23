using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Mostra a dica do objeto interativo que o jogador esta olhando e interage com ele.
    /// </summary>
    public class PlayerInteraction : MonoBehaviour
    {
        private const string InteractableLayerName = "Interactable";
        private const float DisplayCastRadius = 0.1f;

        [SerializeField] private float _raycastDistance = 2f;

        private LayerMask _interactableLayer;
        // Collider cuja dica esta na tela; so atualiza a UI quando o alvo muda
        private Collider _displayedCollider;
        private bool _isHintVisible;

        private void Awake()
        {
            _interactableLayer = LayerMask.GetMask(InteractableLayerName);
            // Forca o primeiro frame sem alvo a esconder a dica, caso a UI comece visivel
            _isHintVisible = true;
        }

        private void OnEnable()
        {
            InputManager.Instance.InteractPressed += HandleInteractPressed;
        }

        private void Update()
        {
            Vector3 origin = transform.position;
            if (!Physics.CapsuleCast(origin, origin + Vector3.up, DisplayCastRadius, transform.forward,
                    out RaycastHit hit, _raycastDistance, _interactableLayer))
            {
                HideHint();
                return;
            }

            // ReferenceEquals compara so a referencia C#, sem a checagem nativa do == do Unity
            Collider hitCollider = hit.collider;
            if (_isHintVisible && ReferenceEquals(hitCollider, _displayedCollider)) return;

            if (hitCollider.TryGetComponent(out IDisplayable displayable))
            {
                displayable.Display();
                _displayedCollider = hitCollider;
                _isHintVisible = true;
            }
            else
            {
                HideHint();
            }
        }

        private void OnDisable()
        {
            InputManager.Instance.InteractPressed -= HandleInteractPressed;
        }

        private void HandleInteractPressed()
        {
            if (!Physics.Raycast(transform.position, transform.forward, out RaycastHit hit, _raycastDistance, _interactableLayer)) return;

            if (hit.collider.TryGetComponent(out IInteractable interactable)) interactable.Interact();
        }

        private void HideHint()
        {
            if (!_isHintVisible) return;

            UIDisplays.Instance.HideKeyInfo();
            _displayedCollider = null;
            _isHintVisible = false;
        }
    }
}
