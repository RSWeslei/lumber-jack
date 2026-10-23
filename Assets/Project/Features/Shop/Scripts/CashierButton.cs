using Managers;
using LumberJack.Core;
public class CashierButton : InteractableButton, IDisplayable
{
    public void Display() 
    {
        UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.InteractKeyLabel} to buy");
    }

    public override void Interact() 
    {
        base.Interact();
        GetComponentInParent<Cashier>()?.Buy();
    }
}
