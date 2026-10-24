using LumberJack.Inventory;
using UnityEngine;

namespace LumberJack.Shop
{
    /// <summary>
    /// Item vendido na loja (dentro de uma caixa): o preço e o item que sai ao abrir a caixa.
    /// </summary>
    [CreateAssetMenu(fileName = "NewBuyable", menuName = "Lumber Jack/Shop/Buyable")]
    public class BuyableDefinition : ItemDefinition
    {
        [Tooltip("Preço em centavos")]
        [SerializeField] private int _priceCents;

        public int PriceCents => _priceCents;
    }
}
