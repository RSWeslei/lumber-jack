using LumberJack.Core;
using LumberJack.Shared;

namespace LumberJack.Shop
{
    /// <summary>
    /// Botão do caixa que paga as caixas que estão no balcão.
    /// </summary>
    public class CashierButton : InteractableButton, IDisplayable
    {
        private Cashier _cashier;
        // Montada uma vez: a tecla não muda durante o jogo e assim a dica não aloca string
        private string _hint;

        private void Start()
        {
            _cashier = GetComponentInParent<Cashier>();
            _hint = $"Press {InputManager.Instance.InteractKeyLabel} to buy";
        }

        public void Display()
        {
            UIDisplays.Instance.ShowKeyInfo(_hint);
        }

        public override void Interact()
        {
            base.Interact();
            _cashier.TryCheckout();
        }
    }
}
