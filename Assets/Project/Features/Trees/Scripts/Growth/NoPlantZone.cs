using UnityEngine;

namespace LumberJack.Trees
{
    /// <summary>
    /// Caixa onde não nasce muda (estradas, entrada da loja, área de venda). Fica como filho de uma ForestArea;
    /// segue a posição e o giro do objeto, e o tamanho vem de <see cref="Size"/>.
    /// </summary>
    public class NoPlantZone : MonoBehaviour
    {
        [Tooltip("Tamanho (m) da zona, centrada neste objeto")]
        [SerializeField] private Vector3 _size = new(6f, 6f, 6f);

        public Vector3 Size => _size;

        public bool Contains(Vector3 position)
        {
            Vector3 local = Quaternion.Inverse(transform.rotation) * (position - transform.position);
            return Mathf.Abs(local.x) <= _size.x * 0.5f && Mathf.Abs(local.y) <= _size.y * 0.5f &&
                   Mathf.Abs(local.z) <= _size.z * 0.5f;
        }
    }
}
