using LumberJack.Core;
using UnityEngine;
using UnityEngine.UIElements;

namespace LumberJack.Shared
{
    /// <summary>
    /// Abre um menu do UI Toolkit (pelo nome do elemento) quando o jogador olha para este objeto.
    /// </summary>
    public class MenuActivator : MonoBehaviour, IDisplayable
    {
        [Tooltip("Objeto com o UIDocument que contém o menu")]
        [SerializeField] private GameObject _document;
        [Tooltip("Nome (name) do VisualElement do menu no UXML")]
        [SerializeField] private string _menuName;

        private VisualElement _menu;
        // Montada uma vez: a tecla não muda durante o jogo e assim a dica não aloca string
        private string _hint;

        private void Start()
        {
            _menu = _document.GetComponent<UIDocument>().rootVisualElement.Q<VisualElement>(_menuName);
            _hint = $"Press {InputManager.Instance.InteractKeyLabel} to open the menu";
        }

        public void Display()
        {
            UIDisplays.Instance.ShowKeyInfo(_hint);
            _menu.style.display = DisplayStyle.Flex;
        }
    }
}
