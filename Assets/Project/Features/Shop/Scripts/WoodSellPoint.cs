using System.Collections.Generic;
using LumberJack.Core;
using LumberJack.Player;
using LumberJack.Wood;
using TMPro;
using UnityEngine;

namespace LumberJack.Shop
{
    /// <summary>
    /// Área de venda de madeira: o pedaço é vendido pelo valor dele (espécie e volume; tábua vale mais) quando está
    /// dentro da área e solto. Enquanto o jogador segura, só espera; ao soltar lá dentro, vende e some.
    /// A placa mostra o valor da última venda.
    /// </summary>
    public class WoodSellPoint : MonoBehaviour
    {
        private const string WoodTag = "Wood";
        private const string LastSaleFormat = "+$ {0:2}";

        [SerializeField] private PlayerStats _playerStats;
        [Tooltip("Quem carrega a madeira: o que está nas mãos só é vendido ao soltar")]
        [SerializeField] private PickupObjects _carrier;
        [Tooltip("Placa com o valor da última venda")]
        [SerializeField] private TextMeshPro _lastSaleText;

        // Pedaços dentro da área e quantos colliders de cada um estão dentro (pedaço de árvore tem vários cascos)
        private readonly Dictionary<Rigidbody, int> _inside = new();
        private readonly List<Rigidbody> _toSell = new();

        private void OnEnable()
        {
            _carrier.HoldChanged += HandleHoldChanged;
        }

        private void Start()
        {
            _lastSaleText.SetText(string.Empty);
        }

        private void OnTriggerEnter(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (!IsWood(body)) return;

            _inside.TryGetValue(body, out int count);
            _inside[body] = count + 1;
            // Jogado ou empurrado para dentro: vende na hora
            if (count == 0) TrySell(body);
        }

        private void OnTriggerExit(Collider other)
        {
            Rigidbody body = other.attachedRigidbody;
            if (!IsWood(body) || !_inside.TryGetValue(body, out int count)) return;

            if (count <= 1) _inside.Remove(body);
            else _inside[body] = count - 1;
        }

        private void OnDisable()
        {
            _carrier.HoldChanged -= HandleHoldChanged;
        }

        // O jogador soltou algo: vende o que ficou dentro da área
        private void HandleHoldChanged()
        {
            if (_carrier.IsHolding || _inside.Count == 0) return;

            _toSell.Clear();
            _toSell.AddRange(_inside.Keys);
            foreach (Rigidbody body in _toSell) TrySell(body);
        }

        private void TrySell(Rigidbody body)
        {
            // Pode ter sido destruído dentro da área (cortado, refinado) sem disparar a saída do trigger
            if (!body)
            {
                _inside.Remove(body);
                return;
            }
            if (_carrier.IsHeld(body)) return;
            if (!body.TryGetComponent(out WoodPiece wood) || wood.ValueCents <= 0 || !wood.TryConsume()) return;

            _inside.Remove(body);
            _playerStats.AddMoney(wood.ValueCents);
            _lastSaleText.SetText(LastSaleFormat, Money.ToUnits(wood.ValueCents));
            Destroy(body.gameObject);
        }

        // Pedaços de árvore colidem com cascos filhos: a madeira fica no objeto do Rigidbody
        private static bool IsWood(Rigidbody body)
        {
            return !ReferenceEquals(body, null) && body.CompareTag(WoodTag);
        }
    }
}
