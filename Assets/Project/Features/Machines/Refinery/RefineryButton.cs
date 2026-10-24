using LumberJack.Core;
using LumberJack.Shared;
using UnityEngine;

namespace LumberJack.Machines
{
    /// <summary>
    /// Botão físico da refinaria que aumenta ou diminui a largura ou a altura da tábua.
    /// </summary>
    public class RefineryButton : InteractableButton, IDisplayable
    {
        [SerializeField] private RefineryDimension _dimension;
        [SerializeField] private bool _isDecrease;

        private WoodRefinery _refinery;
        // Montada uma vez: a tecla não muda durante o jogo e assim a dica não aloca string
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
