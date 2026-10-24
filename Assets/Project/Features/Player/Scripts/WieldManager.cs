using LumberJack.Core;
using UnityEngine;

namespace LumberJack.Player
{
    /// <summary>
    /// Toca a animação "Use" do item equipado ao atacar.
    /// </summary>
    public class WieldManager : MonoBehaviour
    {
        private const string UseAnimationState = "Use";

        [SerializeField] private Hand _rightHand;

        private void OnEnable()
        {
            InputManager.Instance.AttackPressed += HandleAttackPressed;
        }

        private void OnDisable()
        {
            InputManager.Instance.AttackPressed -= HandleAttackPressed;
        }

        private void HandleAttackPressed()
        {
            Animator animator = _rightHand.GetEquippedAnimator();
            if (animator != null) animator.Play(UseAnimationState);
        }
    }
}
