using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Machines
{
    /// <summary>
    /// Botao fisico da refinaria que aumenta ou diminui a largura ou a altura da tabua.
    /// </summary>
    public class RefineryButton : InteractableButton, IDisplayable
    {
        [SerializeField] private RefineryDimension _dimension;
        [SerializeField] private bool _isDecrease;

        private WoodRefinery _refinery;
        // Montada uma vez: a tecla nao muda durante o jogo e assim a dica nao aloca string
        private string _hint;

        private void Start()
        {
            _refinery = GetComponentInParent<WoodRefinery>();

            string action = _isDecrease ? "decrease" : "increase";
            string dimension = _dimension == RefineryDimension.Width ? "width" : "height";
            _hint = $"Press {InputManager.Instance.InteractKeyLabel} to {action} {dimension}";
        }

        public void Display()
        {
            UIDisplays.Instance.ShowKeyInfo(_hint);
        }

        public override void Interact()
        {
            base.Interact();
            _refinery.Adjust(_dimension, _isDecrease ? -1 : 1);
        }
    }
}
