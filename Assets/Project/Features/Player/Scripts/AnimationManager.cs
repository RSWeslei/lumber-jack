using UnityEngine;

namespace LumberJack.Player
{
    public class AnimationManager : MonoBehaviour
    {
        [SerializeField] private Animator _rightHandAnimator;

        public void PlayRightHandAnimation(string animationName)
        {
            _rightHandAnimator.Play(animationName);
        }
    }
}
