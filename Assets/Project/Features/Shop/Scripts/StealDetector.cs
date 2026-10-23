using UnityEngine;

namespace LumberJack.Shop
{
    /// <summary>
    /// Na saida da loja: caixa que passa por aqui sem ter sido paga e destruida.
    /// </summary>
    public class StealDetector : MonoBehaviour
    {
        private const string BuyableTag = "Buyable";

        private void OnTriggerEnter(Collider other)
        {
            if (!other.CompareTag(BuyableTag)) return;
            if (other.TryGetComponent(out Buyable buyable) && !buyable.IsBought) Destroy(other.gameObject);
        }
    }
}
