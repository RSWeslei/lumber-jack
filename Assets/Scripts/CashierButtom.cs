using Managers;
public class CashierButtom : Buttom, IDisplayable
{
    public void Display() 
    {
        UIDisplays.Instance.ShowKeyInfo($"Press {InputManager.Instance.interactKey} to buy");
    }

    public override void Interact() 
    {
        base.Interact();
        GetComponentInParent<Cashier>()?.Buy();
    }
}
