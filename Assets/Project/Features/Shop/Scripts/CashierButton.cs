using LumberJack.Core;

namespace LumberJack.Shop
{
    /// <summary>
    /// Botao do caixa que paga as caixas que estao no balcao.
    /// </summary>
    public class CashierButton : InteractableButton, IDisplayable
    {
        private Cashier _cashier;
        // Montada uma vez: a tecla nao muda durante o jogo e assim a dica nao aloca string
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
