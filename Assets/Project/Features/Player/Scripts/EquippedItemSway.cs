using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Balanca o item na mao de leve na direcao oposta ao movimento.
    /// Adicionado em runtime pela <see cref="Hand"/>.
    /// </summary>
    public class EquippedItemSway : MonoBehaviour
    {
        private const float SwayAmount = 0.04f;
        private const float MaxSway = 0.2f;
        private const float Smoothing = 3f;

        private Vector3 _restPosition;

        private void Start()
        {
            _restPosition = transform.localPosition;
        }

        private void Update()
        {
            Vector2 move = InputManager.Instance.MoveInput;
            Vector3 sway = new Vector3(
                Mathf.Clamp(-move.x * SwayAmount, -MaxSway, MaxSway),
                Mathf.Clamp(-move.y * SwayAmount, -MaxSway, MaxSway),
                0f);

            transform.localPosition = Vector3.Lerp(transform.localPosition, _restPosition + sway, Time.deltaTime * Smoothing);
        }
    }
}
