using LumberJack.Core;
using LumberJack.Shared;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Mostra a dica do objeto interativo que o jogador está mirando e interage com esse
    /// mesmo objeto (o que aparece na dica é o que o botão usa).
    /// </summary>
    [RequireComponent(typeof(PlayerAim))]
    public class PlayerInteraction : MonoBehaviour
    {
        private const string InteractableLayerName = "Interactable";

        private PlayerAim _aim;
        private int _interactableMask;

        // Alvo atual; os componentes só são buscados quando o collider mirado muda
        private Collider _targetCollider;
        private bool _hasTarget;
        private IDisplayable _displayable;
        private bool _hasDisplayable;
        private IInteractable _interactable;
        private bool _hasInteractable;
        private bool _isHintVisible;

        private void Awake()
        {
            _aim = GetComponent<PlayerAim>();
            _interactableMask = LayerMask.GetMask(InteractableLayerName);
            // Força o primeiro frame sem alvo a esconder a dica, caso a UI comece visível
            _isHintVisible = true;
        }

        private void OnEnable()
        {
            InputManager.Instance.InteractPressed += HandleInteractPressed;
        }

        private void Update()
        {
            if (!_aim.TryFindTarget(_interactableMask, out RaycastHit hit))
            {
                ClearTarget();
                return;
            }

            // ReferenceEquals compara só a referência C#, sem a checagem nativa do == do Unity
            Collider hitCollider = hit.collider;
            if (_hasTarget && ReferenceEquals(hitCollider, _targetCollider)) return;

            _targetCollider = hitCollider;
            _hasTarget = true;
            _hasDisplayable = hitCollider.TryGetComponent(out _displayable);
            _hasInteractable = hitCollider.TryGetComponent(out _interactable);

            if (_hasDisplayable)
            {
                _displayable.Display();
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
            if (!_hasTarget || !_hasInteractable) return;

            // O alvo pode ter sido destruído desde o último frame (ex.: item já pego num clique anterior)
            if (!_targetCollider)
            {
                ClearTarget();
                return;
            }

            _interactable.Interact();
        }

        private void ClearTarget()
        {
            _hasTarget = false;
            _targetCollider = null;
            _displayable = null;
            _interactable = null;
            _hasDisplayable = false;
            _hasInteractable = false;
            HideHint();
        }

        private void HideHint()
        {
            if (!_isHintVisible) return;

            UIDisplays.Instance.HideKeyInfo();
            _isHintVisible = false;
        }
    }
}
