using LumberJack.Core;
using LumberJack.Shop;
using TMPro;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.Shared
{
    /// <summary>
    /// Dicas de tecla na tela ("Press E to...") e o painel de informacoes de um produto da loja.
    /// </summary>
    public class UIDisplays : MonoBehaviour
    {
        private const string KeyInfoParentName = "KeyInfoParent";
        private const string KeyInfoLabelName = "KeyInfo";

        [SerializeField] private UIDocument _menuDocument;

        [Header("Painel do produto")]
        [SerializeField] private GameObject _buyablePanel;
        [SerializeField] private TextMeshProUGUI _buyableNameText;
        [SerializeField] private TextMeshProUGUI _buyablePriceText;
        [SerializeField] private TextMeshProUGUI _buyableDescriptionText;

        private VisualElement _keyInfoParent;
        private Label _keyInfoText;
        // So mexe na UI quando a dica muda (as dicas sao strings montadas uma vez, entao comparar a referencia basta)
        private string _shownKeyInfo;
        private bool _isKeyInfoVisible;

        public static UIDisplays Instance { get; private set; }

        private void Awake()
        {
            if (Instance != null && Instance != this)
            {
                Destroy(this);
                return;
            }
            Instance = this;

            _keyInfoParent = _menuDocument.rootVisualElement.Q<VisualElement>(KeyInfoParentName);
            _keyInfoText = _keyInfoParent.Q<Label>(KeyInfoLabelName);
        }

        private void OnDestroy()
        {
            if (Instance == this) Instance = null;
        }

        public void ShowKeyInfo(string keyInfo)
        {
            if (_isKeyInfoVisible && ReferenceEquals(keyInfo, _shownKeyInfo)) return;

            _keyInfoText.text = keyInfo;
            _keyInfoParent.style.display = DisplayStyle.Flex;
            _shownKeyInfo = keyInfo;
            _isKeyInfoVisible = true;
        }

        public void HideKeyInfo()
        {
            if (!_isKeyInfoVisible) return;

            _keyInfoParent.style.display = DisplayStyle.None;
            _isKeyInfoVisible = false;
        }

        public void ShowBuyableInfo(BuyableDefinition buyable)
        {
            _buyableNameText.text = buyable.DisplayName;
            _buyablePriceText.text = Money.Format(buyable.PriceCents);
            _buyableDescriptionText.text = buyable.Description;
            _buyablePanel.SetActive(true);
        }

        public void HideBuyableInfo()
        {
            _buyablePanel.SetActive(false);
        }
    }
}
