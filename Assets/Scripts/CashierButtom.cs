public class CashierButtom : Buttom
{
    public override void Press() {
        base.Press();
        GetComponentInParent<Cashier>()?.Buy();
    }
}
