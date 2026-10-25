using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.Player
{
    /// <summary>
    /// Ponto na HUD em cima do lugar agarrado enquanto o jogador segura algo: verde carregando, vermelho quando é
    /// pesado demais para levantar.
    /// </summary>
    [RequireComponent(typeof(UIDocument))]
    public class CarryIndicator : MonoBehaviour
    {
        private const string DotName = "carry-dot";

        [SerializeField] private PickupObjects _carrier;
        [Tooltip("Vazio = usa a Camera.main")]
        [SerializeField] private Camera _camera;

        [Header("Cores")]
        [Tooltip("Ponto quando dá para carregar")]
        [SerializeField] private Color _liftColor = new(0.45f, 0.9f, 0.35f);
        [Tooltip("Ponto quando é pesado demais")]
        [SerializeField] private Color _tooHeavyColor = new(0.95f, 0.3f, 0.25f);

        private VisualElement _dot;
        private bool _isVisible;

        private void Awake()
        {
            _dot = GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>(DotName);
            if (_camera == null) _camera = Camera.main;
        }

        private void OnEnable()
        {
            _carrier.HoldChanged += HandleHoldChanged;
        }

        private void LateUpdate()
        {
            if (!_isVisible || !_carrier.TryGetGrabPoint(out Vector3 grabPoint)) return;

            // O ponto segue o lugar agarrado, que anda com o objeto (o UXML centraliza o ponto na posição)
            Vector2 position = RuntimePanelUtils.CameraTransformWorldToPanel(_dot.panel, grabPoint, _camera);
            _dot.style.left = position.x;
            _dot.style.top = position.y;
        }

        private void OnDisable()
        {
            _carrier.HoldChanged -= HandleHoldChanged;
        }

        private void HandleHoldChanged()
        {
            _isVisible = _carrier.IsHolding;
            _dot.style.display = _isVisible ? DisplayStyle.Flex : DisplayStyle.None;
            if (_isVisible) _dot.style.backgroundColor = _carrier.IsCarrying ? _liftColor : _tooHeavyColor;
        }
    }
}
