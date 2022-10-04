public class CashierButtom : Buttom
{
    public override void Interact() {
        base.Interact();
        GetComponentInParent<Cashier>()?.Buy();
    }
}
