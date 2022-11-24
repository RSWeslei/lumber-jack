public class CashierButtom : Buttom, IDisplayable
{
    public void Display() 
    {
        UIDisplays.Instance.ShowKeyInfo("Press E to buy");
    }

    public void Hide() {
        return;
    }

    public override void Interact() 
    {
        base.Interact();
        GetComponentInParent<Cashier>()?.Buy();
    }
}
