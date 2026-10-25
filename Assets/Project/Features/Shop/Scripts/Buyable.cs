using LumberJack.Core;
using LumberJack.Shared;
using UnityEngine;

namespace LumberJack.Shop
{
    /// <summary>
    /// Caixa de produto na loja: mostra as informações, e depois de paga no caixa pode ser aberta
    /// para soltar o item.
    /// </summary>
    [RequireComponent(typeof(MeshRenderer))]
    public class Buyable : MonoBehaviour, IDisplayable, IInteractable
    {
        [SerializeField] private BuyableDefinition _definition;
        [Tooltip("Cor da caixa depois de paga")]
        [SerializeField] private Color _boughtColor = Color.green;

        private MeshRenderer _renderer;
        // Montadas uma vez: a tecla não muda durante o jogo e assim a dica não aloca string
        private string _checkHint;
        private string _openHint;

        public BuyableDefinition Definition => _definition;
        public bool IsBought { get; private set; }

        private void Awake()
        {
            _renderer = GetComponent<MeshRenderer>();
        }

        private void Start()
        {
            string key = InputManager.Instance.InteractKeyLabel;
            _checkHint = $"Press {key} to check";
            _openHint = $"Press {key} to open the box";
        }

        public void MarkAsBought()
        {
            IsBought = true;
            _renderer.material.color = _boughtColor;
        }

        public void Display()
        {
            UIDisplays.Instance.ShowKeyInfo(IsBought ? _openHint : _checkHint);
        }

        public void Interact()
        {
            if (IsBought) OpenBox();
            else UIDisplays.Instance.ShowBuyableInfo(_definition);
        }

        private void OpenBox()
        {
            Instantiate(_definition.Prefab, transform.position, Quaternion.identity);
            Destroy(gameObject);
        }
    }
}
